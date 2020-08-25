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
using System.Security.Cryptography;
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
        private readonly IBackendAdminRepository _adminRepository;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly JwtSettings _jwtsettings;

        public BackendAdminAuthController(UserManager<ApplicationUser> userManager, IHttpClientFactory httpClientFactory, IOptions<JwtSettings> jwtsettings,
            ILogger<BackendAdminAuthController> logger, IClassOrRoleRepository roleRepository,IApplicationUserRepository userRepository,IBackendAdminRepository adminRepository,
            TokenValidationParameters tokenValidationParameters)
        {
            _userManager = userManager;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _roleRepository = roleRepository;
            _userRepository = userRepository;
            _adminRepository = adminRepository;
            _tokenValidationParameters = tokenValidationParameters;
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
                        var httpClient = _httpClientFactory.CreateClient("Fiorano");
                        var loginCredentials = new ADCredentialsRoot();
                        loginCredentials.AD_Credentials = aDCredentials;
                        HttpContent content = new StringContent(JsonConvert.SerializeObject(loginCredentials), Encoding.UTF8, "application/json");
                        var authentication = await httpClient.PostAsync("AD/ADAuthentication", content);
                        if (authentication.IsSuccessStatusCode)
                        {
                            string apiResponse = await authentication.Content.ReadAsStringAsync();
                            var result = JsonConvert.DeserializeObject<ADResponseRoot>(apiResponse);
                            if (result.AD_Response.Status == "TRUE" && result.AD_Response.Response.ResponseCode == "00")
                            {
                                //var roles = await _userManager.GetRolesAsync(checkIfUserExist);
                                ////Generate Token
                                //var expirationTime = Convert.ToDouble(_jwtsettings.ExpirationTime);
                                //var tokenHandler = new JwtSecurityTokenHandler();
                                //var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtsettings.Secret));
                                //var tokenDescriptor = new SecurityTokenDescriptor
                                //{
                                //    Subject = new ClaimsIdentity(new[]
                                //    {
                                //new Claim(JwtRegisteredClaimNames.Sub, aDCredentials.AD_Username),
                                //new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                                //new Claim(ClaimTypes.Email,  aDCredentials.AD_Username),
                                //new Claim("FirstName",checkIfUserExist.FirstName as string),
                                //new Claim("LastName",checkIfUserExist.LastName as string),
                                //new Claim(ClaimTypes.Role, roles.FirstOrDefault() as string)
                                //}),
                                //    SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature),
                                //    Issuer = _jwtsettings.Site,
                                //    Audience = _jwtsettings.Audience,
                                //    Expires = DateTime.UtcNow.AddMinutes(expirationTime)
                                //};

                                ////create the token 
                                //var token = tokenHandler.CreateToken(tokenDescriptor);
                                //var loggedInAdminResponseDTO = new LoggedInAdminResponseDTO
                                //{
                                //    Token = tokenHandler.WriteToken(token),
                                //    Username = aDCredentials.AD_Username,
                                //    FirstName = checkIfUserExist.FirstName,
                                //    LastName = checkIfUserExist.LastName,
                                //    Email = checkIfUserExist.Email,
                                //    ExpiryTime = DateTime.Now.AddMinutes(expirationTime),
                                //    Roles = roles,
                                //};
                                var loggedInAdminResponseDTO = await GetAuthenticationResultForUserAsync(checkIfUserExist);
                                return Ok(new ResponseMessage<LoggedInAdminResponseDTO> { Data = loggedInAdminResponseDTO, Status = true, Message = "Login was successfully" });
                            }
                            else
                            {
                                return Unauthorized(new ResponseMessage { Message = "Authentication failed" });
                            }
                        }
                        return BadRequest(new ResponseMessage { Message = "An error occurred when connecting to core ADService" });
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
                        UserName = createAdminViewModel.Email,
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
            try
            {
                var users = await _adminRepository.GetBackendAdmins();
                return Ok(new ResponseMessage<List<BackendAdminUser>>{ Data = users, Status = true, Message = "Admin users was fetched successfully" });
            }
            catch(Exception ex)
            {
                _logger.LogCritical("An error occurred while trying to fetch admin users " + ex);
                return BadRequest(new ResponseMessage { Message = "An error occurred while trying to fetch admin users" });
            }
        }

        //WORKING1
        /// <summary>
        /// Change Admin Role
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "Super-Administrator")]
        [HttpPost("[action]")]
        public async Task<IActionResult> ChangeAdminRole([FromQuery] string email,int roleId)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
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
                        return Ok(new ResponseMessage {Message="Role was changed successfully", Status=true });
                    }
                }
                return NotFound(new ResponseMessage { Message="User does not exist" });
            }
            catch(Exception ex)
            {
                _logger.LogCritical("An error occurred while trying to change admin role " + ex);
                return BadRequest("An error occurred while trying to change to change admin role");
            }           
        }

        //WORKING1
        /// <summary>
        /// Get Admin Roles
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "Super-Administrator")]
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

        //WORKING1
        /// <summary>
        /// Delete Admin
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "Super-Administrator")]
        [HttpGet("[action]")]
        public async Task<IActionResult> RemoveAdmin([FromQuery]string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user != null)
                {
                    var result = _userManager.DeleteAsync(user).Result;
                    if (result.Succeeded)
                    {
                        var admin = await _adminRepository.GetAdminByEmail(email);
                        _adminRepository.Delete(admin);
                        await _adminRepository.Save();
                        return Ok(new ResponseMessage { Message = "Admin was deleted successfully", Status = true });
                    }
                    return BadRequest("An error occurred while trying to change to delete admin");
                }
                return NotFound(new ResponseMessage { Message = "User does not exist" });
            }
            catch(Exception ex)
            {
                _logger.LogCritical("An error occurred while trying to delete admin " + ex);
                return BadRequest("An error occurred while trying to change to delete admin");
            }
        }

        private async Task<LoggedInAdminResponseDTO> GetAuthenticationResultForUserAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            try
            {
                //Generate Token
                var expirationTime = Convert.ToDouble(_jwtsettings.ExpirationTime);
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
                    //Expires = DateTime.Now.AddMinutes(expirationTime)
                    Expires = DateTime.UtcNow.Add(_jwtsettings.TokenLifeTime),

                };
                //create the token 
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var refreshToken = GenerateRefreshToken();

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.Now.AddDays(7);
                _userRepository.Update(user);
                await _userRepository.Save();

                var loggedInAdminResponseDTO = new LoggedInAdminResponseDTO
                {
                    Token = tokenHandler.WriteToken(token),
                    Username = user.UniqueUsername,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    //ExpiryTime = DateTime.Now.AddMinutes(expirationTime),
                    ExpiryTime = DateTime.UtcNow.Add(_jwtsettings.TokenLifeTime),
                    Roles = roles,
                    Success = true,
                    RefreshToken = refreshToken
                };
                return loggedInAdminResponseDTO;
            }
            catch (Exception ex)
            {
                _logger.LogCritical("An error Occurred when " + user.Email + " tried to Login : " + ex);
                return new LoggedInAdminResponseDTO { Errors = new[] { "Error occurred while validating token" } };
            }
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;
            var principal = tokenHandler.ValidateToken(token, _tokenValidationParameters, out securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
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
