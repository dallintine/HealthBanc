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
using Application.Interfaces;
using Application.ViewModels.HealthInsured;
using Application.Services;

namespace HealthBanc.Controllers.V2.BackendAdmin
{
    [Route("v{version:apiVersion}/api/[controller]")]
    [ApiController]
    [ApiVersion("2.0")]

    public class BackendAdminAuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuditLogService _auditLogServices;
        private readonly BackendAdminService _backendAdminService;
        private readonly IRepositoryWrapper _repoWrapper;
        private readonly IEncryptAndDecrypt _encryptDecrypt;
        private readonly ResponseHelper _responseHelper;

        public BackendAdminAuthController(UserManager<ApplicationUser> userManager, AuditLogService auditLogServices, BackendAdminService backendAdminService,IRepositoryWrapper repoWrapper,
            IEncryptAndDecrypt encryptDecrypt,ResponseHelper responseHelper)
        {
            _userManager = userManager;
            _auditLogServices = auditLogServices;
            _backendAdminService = backendAdminService;
            _repoWrapper = repoWrapper;
            _encryptDecrypt = encryptDecrypt;
            _responseHelper = responseHelper;
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
        public async Task<IActionResult> BackendLogin(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var aDCredentials = JsonConvert.DeserializeObject<ADCredentialsViewModel>(decryptedString.Item2);

            var auth = await _backendAdminService.BackendLogin(aDCredentials);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(auth));
            if (auth.Status)
            {
                return Ok(data);
            }
            else if(auth.ResponseCode == 12)
            {
                return Unauthorized(data);
            }
            return BadRequest(data);
        }        

        [HttpPost("[action]")]
        public async Task<IActionResult> BackendRefreshToken(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));

            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var refreshModel = JsonConvert.DeserializeObject<RefreshTokenViewModel>(decryptedString.Item2);
            var authResponse = await _backendAdminService.RefreshToken(refreshModel);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(authResponse));

            if (!authResponse.Status)
            {
                return BadRequest(data);
            }
            return Ok(data);
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
        public async Task<IActionResult> CreateBackendAdmin(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var createAdminViewModel = JsonConvert.DeserializeObject<CreateAdminViewModel>(decryptedString.Item2);

            // Get logged in admin userID
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            string email = User.FindFirst(ClaimTypes.Email)?.Value;
            int Id = int.Parse(userId);            

            var checkRole = User.IsInRole("Super-Administrator");
            if (checkRole)
            {
                var response = await _backendAdminService.CreateBackendAdmin(createAdminViewModel, email, Id);
                var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(response));
                if (response.Status)
                {
                    return Ok(data);
                }
                return BadRequest(data);
            }
            else
            {
                var data2 = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(new ResponseMessage { Message = "You do not have the authority to add a new admin,contact the Super Admin" }));
                return BadRequest(data2);
            }       
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
            var data2 = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(new ResponseMessage<List<BackendAdminUser>> { Data = users, Status = true, Message = "Admin users was fetched successfully" }));
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
        public async Task<IActionResult> ChangeAdminRole(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var model = JsonConvert.DeserializeObject<ChangeAdminRoleViewModel>(decryptedString.Item2);

            //Get logged in admin userid
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            var checkRole = User.IsInRole("Super-Administrator");
            if (checkRole)
            {
                var response = await _backendAdminService.ChangeAdminRole(Id, model.Email, model.RoleId);
                var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(response));

                if (response.Status)
                {
                    return Ok(data);
                }
                return NotFound(data);
            }
            var data2 = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(new ResponseMessage { Message = "You do not have the authority to change admin role,contact the Super Admin" }));
            return BadRequest(data2);
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
        [HttpPost("[action]")]
        public async Task<IActionResult> RemoveAdmin(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var emailModel = JsonConvert.DeserializeObject<EmailViewModel>(decryptedString.Item2);

            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            var checkRole = User.IsInRole("Super-Administrator");
            if (checkRole)
            {
                var response = await _backendAdminService.RemoveAdmin(Id, emailModel.Email);
                var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(response));
                if (response.Status)
                {
                    return Ok(data);
                }
                return BadRequest(data);
            }
            var data2 = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(new ResponseMessage { Message = "You do not have the authority to remove an admin,contact the Super Admin" }));
            return BadRequest(data2);
        }

        /// <summary>
        /// Disable Admin
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        [HttpPost("[action]")]
        public async Task<IActionResult> DisableAdmin(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var emailModel = JsonConvert.DeserializeObject<EmailViewModel>(decryptedString.Item2);

            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            var checkRole = User.IsInRole("Super-Administrator");
            if (checkRole)
            {
                var response = await _backendAdminService.DisableAdmin(Id, emailModel.Email);
                var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(response));

                if (response.Status)
                {
                    return Ok(data);
                }
                return NotFound(data);                
            }
            var data2 = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(new ResponseMessage { Message = "You do not have the authority to disable an admin,contact the Super Admin" }));
            return BadRequest(data2);
        }

        /// <summary>
        /// Enable Admin
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        [HttpPost("[action]")]
        public async Task<IActionResult> EnableAdmin(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var emailModel = JsonConvert.DeserializeObject<EmailViewModel>(decryptedString.Item2);

            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            var checkRole = User.IsInRole("Super-Administrator");
            if (checkRole)
            {
                var response = await _backendAdminService.EnableAdmin(Id, emailModel.Email);
                var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(response));
                if (response.Status)
                {
                    return Ok(data);
                }
                return NotFound(data);
            }
            var data2 = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(new ResponseMessage { Message = "You do not have the authority to enable an admin,contact the Super Admin" }));
            return BadRequest(data2);
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
