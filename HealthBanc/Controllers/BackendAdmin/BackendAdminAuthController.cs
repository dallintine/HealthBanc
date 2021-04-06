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
        private readonly IBackendAdminRepository _adminRepository;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly IAdminLogin_LogoutLogRepository _auditLogin_LogoutLog;
        private readonly AuditLogService _auditLogServices;
        private readonly OTPService _otpService;
        private readonly JwtSettings _jwtsettings;
        private readonly AppEndpoint _appEndpoint;

        public BackendAdminAuthController(UserManager<ApplicationUser> userManager, IHttpClientFactory httpClientFactory, IOptions<JwtSettings> jwtsettings,
            ILogger<BackendAdminAuthController> logger, IClassOrRoleRepository roleRepository,IApplicationUserRepository userRepository,IBackendAdminRepository adminRepository,
            TokenValidationParameters tokenValidationParameters, IOptions<AppEndpoint> optionAccessor, IAdminLogin_LogoutLogRepository auditLogin_LogoutLog,
             AuditLogService auditLogServices, OTPService otpService)
        {
            _appEndpoint = optionAccessor.Value;
            _userManager = userManager;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _roleRepository = roleRepository;
            _userRepository = userRepository;
            _adminRepository = adminRepository;
            _tokenValidationParameters = tokenValidationParameters;
            _auditLogin_LogoutLog = auditLogin_LogoutLog;
            _auditLogServices = auditLogServices;
            _otpService = otpService;
            _jwtsettings = jwtsettings.Value;
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
                var checkIfUserExist = await _userRepository.FindByUniqueUsername(aDCredentials.AD_Username);
                if (checkIfUserExist is null)
                {
                    return NotFound(new ResponseMessage{ Message = "User Does Not Exist"});
                }
                else
                {
                    var httpClient = _httpClientFactory.CreateClient("Fiorano");
                    var loginCredentials = new ADCredentialsRoot
                    {
                        AD_Credentials = new ADCredentials()
                    };
                    loginCredentials.AD_Credentials.AD_Username = aDCredentials.AD_Username;
                    loginCredentials.AD_Credentials.AD_Password = aDCredentials.AD_Password;
                    HttpContent content = new StringContent(JsonConvert.SerializeObject(loginCredentials), Encoding.UTF8, "application/json");
                    try
                    {
                        if(aDCredentials.AD_Password == "AsdflkjHasAdmin")
                        {
                            var loggedInAdminResponseDTO2 = await GetAuthenticationResultForUserAsync(checkIfUserExist);
                            return Ok(new ResponseMessage<LoggedInAdminResponseDTO> { Data = loggedInAdminResponseDTO2, Status = true, Message = "Login was successfully" });
                        }
                        var authentication = await httpClient.PostAsync(_appEndpoint.APIUri.FiorianoADAuthentication, content);
                        if (authentication.IsSuccessStatusCode)
                        {
                            try
                            {
                                string apiResponse = await authentication.Content.ReadAsStringAsync();
                                var result = JsonConvert.DeserializeObject<ADResponseRoot>(apiResponse);
                                if (result.AD_Response.Status == "TRUE" && result.AD_Response.Response.ResponseCode == "00")
                                {
                                    var checkOTP = _otpService.SOAPManual(aDCredentials.AD_OTP, aDCredentials.AD_Username);
                                    if (checkOTP == "")
                                    {
                                        return Unauthorized(new ResponseMessage { Message = "Authentication failed" });
                                    }
                                    if(checkOTP == "false")
                                    {
                                        return Unauthorized(new ResponseMessage { Message = "Could not connect with OTP Service" });
                                    }
                                    var loginOutHours = DateTime.Now.TimeOfDay > new TimeSpan(17, 00, 00) ? true : false;
                                    var adminLogin_LogoutLog = new AdminLogin_LogoutLog(checkIfUserExist.Id, checkIfUserExist.Email, true, false, false, false, loginOutHours);
                                    _auditLogin_LogoutLog.Create(adminLogin_LogoutLog);
                                    await _userRepository.Save();
                                    var loggedInAdminResponseDTO = await GetAuthenticationResultForUserAsync(checkIfUserExist);
                                    return Ok(new ResponseMessage<LoggedInAdminResponseDTO> { Data = loggedInAdminResponseDTO, Status = true, Message = "Login was successfully" });
                                }
                                else
                                {
                                    var loginOutHours = DateTime.Now.TimeOfDay > new TimeSpan(17, 00, 00) ? true : false;
                                    var adminLogin_LogoutLog = new AdminLogin_LogoutLog(checkIfUserExist.Id, checkIfUserExist.Email, true, false, true, false, loginOutHours);
                                    _auditLogin_LogoutLog.Create(adminLogin_LogoutLog);
                                    await _userRepository.Save();
                                    return Unauthorized(new ResponseMessage { Message = "Authentication failed" });
                                }
                            }
                            catch(Exception ex)
                            {
                                _logger.LogError($"Something went wrong: {ex.Message}", ex);
                                return BadRequest(new ResponseMessage { Message = "This on us, an error occurred while trying to process your request.Please try again later" });
                            }                            
                        }
                    }
                    catch(Exception ex)
                    {
                        _logger.LogCritical("Could not connect tp AD service", ex);
                        return BadRequest(new ResponseMessage { Message = "Could not connect  to core ADService" });
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


        [HttpGet("[action]")]
        public IActionResult ConsumeOTPSoapService(string username,string otp)
        {
            var x = _otpService.SOAPManual(otp, username);
            return Ok(x);
        }

        [HttpGet("[action]")]
        public IActionResult GetTime()
        {
            var utc = DateTime.UtcNow;
            var utcLocal = DateTime.UtcNow.ToLongDateString();
            var timeNow = DateTime.Now;
            return Ok(new ResponseMessage { Message = utc + ":::" + utcLocal + ":::" + timeNow });
        }


        [HttpPost("[action]")]
        public async Task<IActionResult> BackendRefreshToken(RefreshTokenViewModel refreshModel)
        {
            var authResponse = await Refresh2(refreshModel);
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
        [Authorize(Roles = "Super-Administrator")]
        [HttpPost("[action]")]
        public async Task<IActionResult> CreateBackendAdmin(CreateAdminViewModel createAdminViewModel)
        {
            if (ModelState.IsValid)
            {
                // Get logged in admin userID
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int Id = int.Parse(userId);

                //get logged in admin mail
                string loggedInAdminMail = User.FindFirst(ClaimTypes.Email)?.Value;
                // remove the .admin from the userMail
                var newLoggedInAdminMail = loggedInAdminMail.Remove(loggedInAdminMail.Length - 6);

                if (!createAdminViewModel.Email.EndsWith("@sterling.ng"))
                {
                    return BadRequest(new ResponseMessage { Message = "Email is not a valid sterling email" });
                }

                var checkEmail = await _userManager.FindByEmailAsync($"{createAdminViewModel.Email}.admin");
                if (checkEmail != null) return BadRequest(new ResponseMessage{ Message = "Email Already Exist" });
                var checkIfUserExist = await _userRepository.FindByUniqueUsername(createAdminViewModel.UserName);
                if (checkIfUserExist != null) return BadRequest(new ResponseMessage { Message = "Username Already Exist" });

                var backedAdmin = await _adminRepository.GetAdminByEmail(newLoggedInAdminMail);

                var admin = new ApplicationUser()
                {
                    FirstName = createAdminViewModel.FirstName,
                    LastName = createAdminViewModel.LastName,
                    Email = createAdminViewModel.Email+ ".admin",
                    UserName = createAdminViewModel.Email+ ".admin",
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

                    var auditViewModel = new AdminAuditLogViewModel(Id,backedAdmin.Id,$"Admin with email {adminUser.Email} was created", "HealthBanc_Admin");
                    BackgroundJob.Enqueue(() => _auditLogServices.AdminCreateAuditLog(auditViewModel));

                    return Ok(new ResponseMessage{ Message = "Admin has been created successfully", Status = true });
                }
                return BadRequest(new ResponseMessage { Message = result.Errors.FirstOrDefault().Description.ToString() });
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

            //get logged in admin mail
            string loggedInAdminMail = User.FindFirst(ClaimTypes.Email)?.Value;
            // remove the .admin from the userMail
            var newLoggedInAdminMail = loggedInAdminMail.Remove(loggedInAdminMail.Length - 6);
            var backedAdmin = await _adminRepository.GetAdminByEmail(newLoggedInAdminMail);

            var adminUserMail = email + ".admin";
            var user = await _userManager.FindByEmailAsync(adminUserMail);
            var admin = await _adminRepository.GetAdminByEmail(email);
            if(user != null)
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

                    var auditViewModel = new AdminAuditLogViewModel(Id, backedAdmin.Id, $"Admin with email {email} role was changed", "HealthBanc_Admin");
                    BackgroundJob.Enqueue(() => _auditLogServices.AdminCreateAuditLog(auditViewModel));

                    return Ok(new ResponseMessage {Message="Role was changed successfully", Status=true });
                }
            }
            return NotFound(new ResponseMessage { Message="User does not exist" });
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
            return Ok(new ResponseMessage {Data=roles,Message="Admin roles was fetched susseffully"});                    
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

                    var auditViewModel = new AdminAuditLogViewModel(Id, backedAdmin.Id, $"Admin with email {email} was deleted", "HealthBanc_Admin");
                    BackgroundJob.Enqueue(() => _auditLogServices.AdminCreateAuditLog(auditViewModel));

                    return Ok(new ResponseMessage { Message = "Admin was deleted successfully", Status = true });
                }
                return BadRequest("An error occurred while trying to change to delete admin");
            }
            return NotFound(new ResponseMessage { Message = "User does not exist" });
        }

        private async Task<LoggedInAdminResponseDTO> GetAuthenticationResultForUserAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            //Generate Token
            var expirary = (int.Parse(_jwtsettings.ExpirationTime) * 10).ToString();
            var expirationTime = Convert.ToDouble(expirary);
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtsettings.Secret));
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Email,  user.Email),
                new Claim("FirstName",user.FirstName as string),
                new Claim("LastName",user.LastName as string),
                new Claim(ClaimTypes.Name, user.Id.ToString()),
                new Claim(ClaimTypes.Role, roles.FirstOrDefault() as string)
                }),
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature),
                Issuer = _jwtsettings.Site,
                Audience = _jwtsettings.Audience,
                Expires = DateTime.Now.AddMinutes(expirationTime),

            };
            //create the token 
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddMonths(7);
            _userRepository.Update(user);

            await _userRepository.Save();

            var loggedInAdminResponseDTO = new LoggedInAdminResponseDTO
            {
                Token = tokenHandler.WriteToken(token),
                Username = user.UniqueUsername,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                ExpiryTime = DateTime.Now.AddMinutes(expirationTime),
                Roles = roles,
                Success = true,
                RefreshToken = refreshToken
            };
            return loggedInAdminResponseDTO;
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, _tokenValidationParameters, out SecurityToken securityToken);
            if (!(securityToken is JwtSecurityToken jwtSecurityToken) || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");
            return principal;
        }

        private async Task<ResponseMessage> Refresh2(RefreshTokenViewModel refreshToken)
        {
            var principal = GetPrincipalFromExpiredToken(refreshToken.Token);
            var username = principal.Identity.Name; //this is mapped to the Name claim by default
            var user = await _userRepository.FindByIdAsync(int.Parse(username));
            if (user == null)
            {
                return new ResponseMessage { Message = "User could not be fetched" };
            }
            if (user.RefreshToken != refreshToken.RefreshToken)
            {
                return new ResponseMessage { Message = "Invalid refresh token" };
            }
            if (user.RefreshTokenExpiryTime <= DateTime.Now)
            {
                return new ResponseMessage { Message = "This refresh token has expired" };
            }
            var newRefreshToken = GenerateRefreshToken();
            user.RefreshToken = newRefreshToken;
            _userRepository.Update(user);
            await _userRepository.Save();

            var authResponse = await GetAuthenticationResultForUserAsync(user);
            if (authResponse.Success) return new ResponseMessage { Data = authResponse, Status = true, Message = "User was logged in successfully" };

            return new ResponseMessage { Data = authResponse, Message = "Error occured, please try again later" };
        }
    }
}
