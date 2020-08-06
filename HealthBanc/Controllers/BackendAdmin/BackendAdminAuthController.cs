using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Domain.Models;
using HealthBanc.DTO.AuthenticationDTOs;
using HealthBanc.Helpers.Jwt_Authorization;
using HealthBanc.Response;
using HealthBanc.ViewModels;
using Microsoft.AspNet.OData;
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
using System.Text;
using System.Threading.Tasks;

namespace HealthBanc.Controllers
{
    [Route("v1/api/[controller]")]
    [ApiController]

    public class BackendAdminAuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<BackendAdminAuthController> _logger;
        private readonly IClassOrRoleRepository _roleRepository;
        private readonly IApplicationUserRepository _userRepository;
        private readonly JwtSettings _jwtsettings;

        public BackendAdminAuthController(UserManager<ApplicationUser> userManager, IHttpClientFactory httpClientFactory, IOptions<JwtSettings> jwtsettings,
            ILogger<BackendAdminAuthController> logger, IClassOrRoleRepository roleRepository,IApplicationUserRepository userRepository)
        {
            _userManager = userManager;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _roleRepository = roleRepository;
            _userRepository = userRepository;
            _jwtsettings = jwtsettings.Value;
        }


        //WORKING1
        /// <summary>
        /// Logs the BackendUser In
        /// </summary>
        /// <returns>returns LoggedInResponse Object</returns>
        /// <response code="200"> Return LoggedInResponse Object</response>
        /// <response code="401">Success : Username or password invalid, please try again with correct details.</response>
        /// <response code="400">Error : List of Input Validation Errors </response>
        /// <response code="404"> Error : User Does Not Exist</response>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage))]
        [ProducesResponseType(401, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public async Task<IActionResult> BackendLogin([FromBody] ADCredentials aDCredentials)
        {
            if (ModelState.IsValid)
            {
                var checkIfUserExist = await _userRepository.FindByUniqueUsername(aDCredentials.AD_Username);
                if (checkIfUserExist is null)
                {
                    return NotFound(new ResponseMessage{ Message = "User Does Not Exist"});
                }
                else
                {
                    try
                    {
                        //var httpClient = _httpClientFactory.CreateClient("Fiorano");
                        //var loginCredentials = new ADCredentialsRoot();
                        //loginCredentials.AD_Credentials = aDCredentials;
                        //HttpContent content = new StringContent(JsonConvert.SerializeObject(loginCredentials), Encoding.UTF8, "application/json");
                        //var authentication = await httpClient.PostAsync("AD/ADAuthentication", content);
                        //string apiResponse = await authentication.Content.ReadAsStringAsync();
                        //var result = JsonConvert.DeserializeObject<ADResponseRoot>(apiResponse);
                        if (/*result.AD_Response.ResponseCode == "00"*/ true)
                        {
                            var roles = await _userManager.GetRolesAsync(checkIfUserExist);
                            //Generate Token
                            var expirationTime = Convert.ToDouble(_jwtsettings.ExpirationTime);
                            var tokenHandler = new JwtSecurityTokenHandler();
                            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtsettings.Secret));
                            var tokenDescriptor = new SecurityTokenDescriptor
                            {
                                Subject = new ClaimsIdentity(new[]
                                {
                                new Claim(JwtRegisteredClaimNames.Sub, aDCredentials.AD_Username),
                                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                                new Claim(ClaimTypes.Email,  aDCredentials.AD_Username),
                                new Claim("FirstName",checkIfUserExist.FirstName as string),
                                new Claim("LastName",checkIfUserExist.LastName as string),
                                new Claim(ClaimTypes.Role, roles.FirstOrDefault() as string)
                                }),
                                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature),
                                Issuer = _jwtsettings.Site,
                                Audience = _jwtsettings.Audience,
                                Expires = DateTime.UtcNow.AddMinutes(expirationTime)
                            };

                            //create the token 
                            var token = tokenHandler.CreateToken(tokenDescriptor);
                            var loggedInResponseDTO = new LoggedInResponseDTO
                            {
                                Token = tokenHandler.WriteToken(token),
                                Username = aDCredentials.AD_Username,
                                ExpiryTime = DateTime.Now.AddMinutes(expirationTime),
                                Roles = roles,
                            };
                            return Ok(new ResponseMessage{ Data = loggedInResponseDTO, Status = true,Message="Login was successfully" });
                        }
                        else
                        {
                            return Unauthorized(new ResponseMessage { Message = "Username or password invalid, please try again with correct details." });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical("Error occured when trying to Login backend admin" + ex);
                        return BadRequest(new ResponseMessage{ Message = "Sorry for the inconvenience please try again or contact support" });
                    }
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
            return BadRequest(new ResponseMessage{ Data = errors,Status=false,Message="Please check for validation errors" });
        }

        //WORKING1
        /// <summary>
        /// Creates the BackendUser 
        /// </summary>
        /// <response code="200"> Return LoggedInResponse Object</response>
        /// <response code="401">Success : Username or password invalid, please try again with correct details.</response>
        /// <response code="400">Error : List of Input Validation Errors </response>
        /// <response code="404"> Error : User Does Not Exist</response>

        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(401, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage))]
        [Authorize]
        [HttpPost("[action]")]
        public async Task<IActionResult> CreateBackendAdmin(CreateAdminViewModel createAdminViewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var checkEmail = await _userManager.FindByEmailAsync(createAdminViewModel.Email);
                    if (checkEmail != null) return BadRequest(new ResponseMessage{ Message = "Email Already Exist" });
                    var checkIfUserExist = await _userRepository.FindByUniqueUsername(createAdminViewModel.UserName);
                    if (checkIfUserExist != null) return BadRequest(new ResponseMessage { Message = "Username Already Exist" });

                    var admin = new ApplicationUser()
                    {
                        FirstName = createAdminViewModel.FirstName,
                        LastName = createAdminViewModel.LastName,
                        Email = createAdminViewModel.Email,
                        UniqueUsername = createAdminViewModel.UserName,
                        EmailConfirmed = true
                    };
                    var result = _userManager.CreateAsync(admin).Result;
                    if (result.Succeeded)
                    {
                        var role = await _roleRepository.GetRole(createAdminViewModel.RoleId);
                        await _userManager.AddToRoleAsync(checkEmail, role.Name);
                        return Ok(new ResponseMessage{ Message = "Admin has been created successfully", Status = true });
                    }
                }
                catch(Exception ex)
                {
                    _logger.LogCritical("And error occurred while trying to creat backend admin: " + ex);
                    return BadRequest(new ResponseMessage{ Message = "An error occurred while trying to create admin,please try again or contact admin" });
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
            return BadRequest(new ResponseMessage { Data = errors, Status = false, Message = "Please check for validation errors" });
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetAdminRoles()
        {
            try
            {
                var roles = await _roleRepository.GetAdminRoles();
                return Ok(new ResponseMessage {Data=roles,Message="Admin roles was fetched susseffully"});
            }
            catch(Exception ex)
            {
                _logger.LogCritical("An error occurred while trying to get admin roles: " + ex);
                return BadRequest(new ResponseMessage { Message = "An error occurred while trying to get admin roles" });
            }           
        }
    }
}
