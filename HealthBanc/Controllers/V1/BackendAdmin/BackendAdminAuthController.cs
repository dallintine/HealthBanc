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
using DataAccess;

namespace HealthBanc.Controllers
{
    [Route("v{version:apiVersion}/api/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]

    public class BackendAdminAuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuditLogService _auditLogServices;
        private readonly BackendAdminService _backendAdminService;
        private readonly IRepositoryWrapper _repoWrapper;

        public BackendAdminAuthController(UserManager<ApplicationUser> userManager, AuditLogService auditLogServices, BackendAdminService backendAdminService,IRepositoryWrapper repoWrapper)
        {
            _userManager = userManager;
            _auditLogServices = auditLogServices;
            _backendAdminService = backendAdminService;
            _repoWrapper = repoWrapper;
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
                    var response = await _backendAdminService.CreateBackendAdmin(createAdminViewModel, email, Id);
                    if (response.Status)
                    {
                        return Ok(response);
                    }
                    return BadRequest(response);
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
            var users = await _repoWrapper.BackendAdmin.GetBackendAdmins();
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

            var checkRole = User.IsInRole("Super-Administrator");
            if (checkRole)
            {
                var response = await _backendAdminService.ChangeAdminRole(Id, email, roleId);
                if (response.Status)
                {
                    return Ok(response);
                }
                return NotFound(response);
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
            var roles = await _repoWrapper.ClassOrRole.GetAdminRoles();
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

            var checkRole = User.IsInRole("Super-Administrator");
            if (checkRole)
            {
                var response = await _backendAdminService.RemoveAdmin(Id, email);
                if (response.Status)
                {
                    return Ok(response);
                }
                return BadRequest(response);
            }
            return BadRequest(new ResponseMessage { Message = "You do not have the authority to remove an admin,contact the Super Admin" });
        }

        /// <summary>
        /// Disable Admin
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        [HttpGet("[action]")]
        public async Task<IActionResult> DisableAdmin([FromQuery] string email)
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            var checkRole = User.IsInRole("Super-Administrator");
            if (checkRole)
            {
                var response = await _backendAdminService.DisableAdmin(Id, email);
                if (response.Status)
                {
                    return Ok(response);
                }
                return NotFound(response);                
            }
            return BadRequest(new ResponseMessage { Message = "You do not have the authority to disable an admin,contact the Super Admin" });
        }

        /// <summary>
        /// Enable Admin
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        [HttpGet("[action]")]
        public async Task<IActionResult> EnableAdmin([FromQuery] string email)
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            var checkRole = User.IsInRole("Super-Administrator");
            if (checkRole)
            {
                var response = await _backendAdminService.EnableAdmin(Id, email);
                if (response.Status)
                {
                    return Ok(response);
                }
                return NotFound(response);
            }
            return BadRequest(new ResponseMessage { Message = "You do not have the authority to enable an admin,contact the Super Admin" });
        }

        [HttpGet("[action]")]
        [Authorize(Roles = "Super-Administrator")]
        public async Task<IActionResult> GetAllAdminBackendRoles()
        {
            var roles = await _repoWrapper.ClassOrRole.GetAdminRoles();
            return Ok(new ResponseMessage { Data = roles, Message = "Admin roles was fetched susseffully" });                
        }
    }
}
