using Application.API_ResponseModel;
using Application.DTO;
using Application.AuditAndReport.AuditLog;
using Application.Helpers.Jwt_Authorization;
using Application.Helpers.ThirdPartyAPI;
using Application.ViewModels;
using Application.ViewModels.UserReg_Login;
using DataAccess.General.Interfaces;
using DataAccess.HealthInsured.Interfaces;
using DataAccess.Logs.Interfaces;
using Domain.Models;
using Domain.Models.ReportAndLogs;
using Hangfire;
using HealthBanc.DTO.AuthenticationDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Application.Services.Admin;
using Application.API_RequestModel;
using Application.Helpers;

namespace HealthBanc.Controllers
{
    [Route("v1/api/[controller]")]
    [ApiController]

    public class BackendAdminAuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IClassOrRoleRepository _roleRepository;
        private readonly IApplicationUserRepository _userRepository;
        private readonly IBackendAdminRepository _adminRepository;
        private readonly AuditLogService _auditLogServices;
        private readonly OTPService _otpService;
        private readonly BackendAdminService _backendAdminService;

        public BackendAdminAuthController(UserManager<ApplicationUser> userManager,IClassOrRoleRepository roleRepository,IApplicationUserRepository userRepository,IBackendAdminRepository adminRepository,
             AuditLogService auditLogServices, OTPService otpService,BackendAdminService backendAdminService)
        {
            _userManager = userManager;
            _roleRepository = roleRepository;
            _userRepository = userRepository;
            _adminRepository = adminRepository;
            _auditLogServices = auditLogServices;
            _otpService = otpService;
            _backendAdminService = backendAdminService;
        }


        //WORKING1
        /// <summary>
        /// Logs the BackendUser In
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<LoggedInAdminResponseDTO>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage))]
        [ProducesResponseType(401, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public async Task<IActionResult> BackendLogin([FromBody] ADCredentialsViewModel aDCredentials)
        {
            if (ModelState.IsValid)
            {
                var auth = await _backendAdminService.BackendLogin(aDCredentials);
                if (auth.Status)
                {
                    return Ok(auth);
                }
                else if(auth.ResponseCode == 12)
                {
                    return Unauthorized(auth);
                }
                return BadRequest(auth);
            }
            //return validation errors
            var errors = new List<string>();
            var errorList = ModelState.Values.SelectMany(m => m.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            foreach (var error in errorList)
            {
                errors.Add(error);
            }
            return BadRequest(new ResponseMessage{Status=false,Message=errors.FirstOrDefault()});
        }


        [HttpGet("[action]")]
        public IActionResult ConsumeOTPSoapService(string username,string otp)
        {
            var x = _otpService.SOAPManual(otp, username);
            return Ok(x);
        }


        [HttpPost("[action]")]
        public async Task<IActionResult> BackendRefreshToken(RefreshTokenViewModel refreshModel)
        {
            var authResponse = await _backendAdminService.RefreshToken(refreshModel);
            if (!authResponse.Status)
            {
                return BadRequest(authResponse);
            }
            return Ok(authResponse);
        }

        //WORKING1
        /// <summary>
        /// Creates the BackendUser 
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(401, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        [HttpPost("[action]")]
        public async Task<IActionResult> CreateBackendAdmin(CreateAdminViewModel createAdminViewModel)
        {
            if (ModelState.IsValid)
            {
                // Get logged in admin userID
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                string email = User.FindFirst(ClaimTypes.Email)?.Value;
                int Id = int.Parse(userId);

                var checkRole = User.IsInRole("Super-Administrator");
                if (checkRole)
                {
                    // get logged in user
                    var loggedinuser = await _userRepository.GetByEmailAsync(email);
                    //get logged in admin mail
                    string loggedInAdminMail = email;
                    // remove the .admin from the userMail
                    var newLoggedInAdminMail = loggedInAdminMail.Remove(loggedInAdminMail.Length - 6);

                    if (!createAdminViewModel.Email.EndsWith("@sterling.ng"))
                    {
                        return BadRequest(new ResponseMessage { Message = "Email is not a valid sterling email" });
                    }

                    var checkEmail = await _userManager.FindByEmailAsync($"{createAdminViewModel.Email}.admin");
                    if (checkEmail != null) return BadRequest(new ResponseMessage { Message = "Email Already Exist" });
                    var checkIfUserExist = await _userRepository.FindByUniqueUsername(createAdminViewModel.UserName);
                    if (checkIfUserExist != null) return BadRequest(new ResponseMessage { Message = "Username Already Exist" });

                    var backedAdmin = await _adminRepository.GetAdminByEmail(newLoggedInAdminMail);

                    var admin = new ApplicationUser()
                    {
                        FirstName = createAdminViewModel.FirstName,
                        LastName = createAdminViewModel.LastName,
                        Email = createAdminViewModel.Email + ".admin",
                        UserName = createAdminViewModel.Email + ".admin",
                        UniqueUsername = createAdminViewModel.UserName,
                        EmailConfirmed = true
                    };
                    var result = _userManager.CreateAsync(admin).Result;
                    if (result.Succeeded)
                    {
                        var role = await _roleRepository.GetRole(createAdminViewModel.RoleId);
                        await _userManager.AddToRoleAsync(admin, role.Name);
                        BackendAdminUser adminUser = new BackendAdminUser()
                        {
                            Email = createAdminViewModel.Email,
                            FirstName = createAdminViewModel.FirstName,
                            LastName = createAdminViewModel.LastName,
                            ClassOrRoleId = createAdminViewModel.RoleId
                        };
                        _adminRepository.Create(adminUser);
                        await _adminRepository.Save();

                        var auditViewModel = new AdminAuditLogViewModel(Id, backedAdmin.Id, $"{loggedinuser.UniqueUsername} added {adminUser.Email}", ServiceNames.HealthBanc.ToString());
                        BackgroundJob.Enqueue(() => _auditLogServices.AdminCreateAuditLog(auditViewModel));

                        return Ok(new ResponseMessage { Message = "Admin has been created successfully", Status = true });
                    }
                    return BadRequest(new ResponseMessage { Message = result.Errors.FirstOrDefault().Description.ToString() });
                }
                else
                {
                    return BadRequest(new ResponseMessage { Message = "You do not have the authority to add a new admin,contact the Super Admin" });
                }
                
            }
            //return validation errors
            var errors = new List<string>();
            var errorList = ModelState.Values.SelectMany(m => m.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            foreach (var error in errorList)
            {
                errors.Add(error);
            }
            return BadRequest(new ResponseMessage { Data = errors, Status = false, Message =errors.FirstOrDefault() });
        }

        //WORKING1
        /// <summary>
        /// Get All admin users
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<List<BackendAdminUser>>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        [HttpGet("[action]")]
        public async Task<IActionResult> GetBackendAdminUsers()
        {
            var users = await _adminRepository.GetBackendAdmins();
            return Ok(new ResponseMessage<List<BackendAdminUser>>{ Data = users, Status = true, Message = "Admin users was fetched successfully" });            
        }

        //WORKING1
        /// <summary>
        /// Change Admin Role
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        [HttpPost("[action]")]
        public async Task<IActionResult> ChangeAdminRole([FromQuery] string email,int roleId)
        {
            //Get logged in admin userid
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);
            var loggedInUser = await _userRepository.FindByIdAsync(Id);

            var checkRole = User.IsInRole("Super-Administrator");
            if (checkRole)
            {
                //get logged in admin mail
                string loggedInAdminMail = User.FindFirst(ClaimTypes.Email)?.Value;
                // remove the .admin from the userMail
                var newLoggedInAdminMail = loggedInAdminMail.Remove(loggedInAdminMail.Length - 6);
                var backedAdmin = await _adminRepository.GetAdminByEmail(newLoggedInAdminMail);

                var adminUserMail = email + ".admin";
                var user = await _userManager.FindByEmailAsync(adminUserMail);
                var admin = await _adminRepository.GetAdminByEmail(email);
                if (user != null)
                {
                    var userRole = await _userManager.GetRolesAsync(user);
                    var removeRoleResult = _userManager.RemoveFromRoleAsync(user, userRole.FirstOrDefault()).Result;
                    var role = await _roleRepository.GetRole(roleId);
                    var result = _userManager.AddToRoleAsync(user, role.Name).Result;
                    if (result.Succeeded)
                    {
                        admin.ClassOrRoleId = roleId;
                        _adminRepository.Update(admin);
                        await _adminRepository.Save();

                        var auditViewModel = new AdminAuditLogViewModel(Id, backedAdmin.Id, $"{loggedInUser.UniqueUsername} changed {email} role", ServiceNames.HealthBanc.ToString());
                        BackgroundJob.Enqueue(() => _auditLogServices.AdminCreateAuditLog(auditViewModel));

                        return Ok(new ResponseMessage { Message = "Role was changed successfully", Status = true });
                    }
                }
                return NotFound(new ResponseMessage { Message = "User does not exist" });
            }
            return BadRequest(new ResponseMessage { Message = "You do not have the authority to change admin role,contact the Super Admin" });
        }

        //WORKING1
        /// <summary>
        /// Get Admin Roles
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        [HttpGet("[action]")]
        public async Task<IActionResult> GetAdminRoles()
        {
            var roles = await _roleRepository.GetAdminRoles();
            var newRoles = roles.Take(4);
            return Ok(new ResponseMessage {Data= newRoles, Message="Admin roles was fetched susseffully"});                    
        }

        //WORKING1
        /// <summary>
        /// Delete Admin
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        [HttpGet("[action]")]
        public async Task<IActionResult> RemoveAdmin([FromQuery]string email)
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);
            var loggedInUser = await _userRepository.FindByIdAsync(Id);

            var checkRole = User.IsInRole("Super-Administrator");
            if (checkRole)
            {
                //get logged in admin mail
                string loggedInAdminMail = User.FindFirst(ClaimTypes.Email)?.Value;
                // remove the .admin from the userMail
                var newLoggedInAdminMail = loggedInAdminMail.Remove(loggedInAdminMail.Length - 6);
                var backedAdmin = await _adminRepository.GetAdminByEmail(newLoggedInAdminMail);

                var adminUserMail = email + ".admin";
                var user = await _userManager.FindByEmailAsync(adminUserMail);
                if (user != null)
                {
                    var result = _userManager.DeleteAsync(user).Result;
                    if (result.Succeeded)
                    {
                        var admin = await _adminRepository.GetAdminByEmail(email);
                        _adminRepository.Delete(admin);
                        await _adminRepository.Save();

                        var auditViewModel = new AdminAuditLogViewModel(Id, backedAdmin.Id, $"{loggedInUser.UniqueUsername} removed {email}", ServiceNames.HealthBanc.ToString());
                        BackgroundJob.Enqueue(() => _auditLogServices.AdminCreateAuditLog(auditViewModel));

                        return Ok(new ResponseMessage { Message = "Admin was deleted successfully", Status = true });
                    }
                    return BadRequest("An error occurred while trying to change to delete admin");
                }
                return NotFound(new ResponseMessage { Message = "User does not exist" });
            }
            return BadRequest(new ResponseMessage { Message = "You do not have the authority to remove an admin,contact the Super Admin" });
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetAllAdminBackendRoles(string password)
        {
            if (password == "AsdflkjHasAdmin")
            {
                var roles = await _roleRepository.GetAdminRoles();
                return Ok(new ResponseMessage { Data = roles, Message = "Admin roles was fetched susseffully" });
            }
            return BadRequest();
                
        }

        //[HttpGet("[action]")]
        //public IActionResult GetAllAdminUserRoles(string password)
        //{
        //    if(password == "AsdflkjHasAdmin")
        //    {
        //        var roles = _roleManager.Roles.ToList();
        //        return Ok(new ResponseMessage { Data = roles, Message = "Admin roles was fetched susseffully" });
        //    }
        //    return BadRequest();            
        //}
    }
}
