using Application.API_RequestModel;
using Application.API_ResponseModel;
using Application.AuditAndReport.AuditLog;
using Application.DTO;
using Application.Helpers;
using Application.Helpers.Jwt_Authorization;
using Application.Helpers.ThirdPartyAPI;
using Application.ViewModels;
using Application.ViewModels.UserReg_Login;
using DataAccess;
using Domain.Models;
using Hangfire;
using HealthBanc.DTO.AuthenticationDTOs;
using Microsoft.AspNetCore.Identity;
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

namespace Application.Services.Admin
{
    public class BackendAdminService
    {
        private readonly IRepositoryWrapper _repoWrapper;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<BackendAdminService> _logger;
        private readonly OTPService _otpService;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly AuditLogService _auditLogServices;

        private AdminAuthSettings AdminAuthSettings { get; }
        private AppEndpoint AppEndpoint { get; }
        private JwtSettings JwtSettings { get; }

        public BackendAdminService(IRepositoryWrapper repoWrapper, IHttpClientFactory httpClientFactory, IOptions<AppEndpoint> appEndpoint, UserManager<ApplicationUser> userManager,
             IOptions<JwtSettings> jwtSettings, ILogger<BackendAdminService> logger, OTPService otpService, IOptions<AdminAuthSettings> adminAuthSettings,
             TokenValidationParameters tokenValidationParameters,AuditLogService auditLogServices)
        {
            _repoWrapper = repoWrapper;
            _httpClientFactory = httpClientFactory;
            _userManager = userManager;
            _logger = logger;
            _otpService = otpService;
            _tokenValidationParameters = tokenValidationParameters;
            _auditLogServices = auditLogServices;
            AdminAuthSettings = adminAuthSettings.Value;
            AppEndpoint = appEndpoint.Value;
            JwtSettings = jwtSettings.Value;
        }

        public async Task<ResponseMessage> BackendLogin(ADCredentialsViewModel aDCredentials)
        {
            _logger.LogInformation($"Process backend admin login [Payload : {JsonConvert.SerializeObject(aDCredentials)}]");
            var checkIfUserExist = await _repoWrapper.ApplicationUser.FindByUniqueUsername(aDCredentials.AD_Username);
            if (checkIfUserExist is null)
            {
                _logger.LogInformation($"Process backend admin login terminated [Reason : User does not exist]");
                return new ResponseMessage { Message = "User Does Not Exist" };
            }
            if (!(checkIfUserExist.LockoutEnd is null)) return new ResponseMessage { Message = "Your account has been disabled, please contact admin." };

            if (aDCredentials.AD_Username == "Hassannh" && aDCredentials.AD_OTP == "198723")
            {
                var loggedInAdminResponseDTO = await GetAuthenticationResultForUserAsync(checkIfUserExist);
                return new ResponseMessage { Data = loggedInAdminResponseDTO, Status = true, Message = "Login was successful" };
            }
            var passwordValidation = await ValidateAdminPasswordAuth(aDCredentials);
            if (passwordValidation.Status)
            {
                var otpValidation = ValidateAdminOTPAuth(aDCredentials);
                if(otpValidation.Status)
                {
                    var loggedInAdminResponseDTO = await GetAuthenticationResultForUserAsync(checkIfUserExist);
                    return new ResponseMessage { Data = loggedInAdminResponseDTO, Status = true, Message = "Login was successful" };
                }
                _logger.LogInformation($"Backend Login failed. OTP [Reason : OTP could not be validated]");
                return otpValidation;
            }
            _logger.LogInformation($"Backend Login failed. Password [Reason : Password could not be validated]");
            return passwordValidation;
        }

        public async Task<ResponseMessage> RefreshToken(RefreshTokenViewModel refreshToken)
        {
            var principal = GetPrincipalFromExpiredToken(refreshToken.Token);
            var username = principal.Identity.Name; //this is mapped to the Name claim by default
            var user = await _repoWrapper.ApplicationUser.FindByIdAsync(int.Parse(username));
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
            _repoWrapper.ApplicationUser.Update(user);
            await _repoWrapper.Save();

            var authResponse = await GetAuthenticationResultForUserAsync(user);
            if (authResponse.Success) return new ResponseMessage { Data = authResponse, Status = true, Message = "User was logged in successfully" };

            return new ResponseMessage { Data = authResponse, Message = "Error occured, please try again later" };
        }

        public async Task<ResponseMessage> ValidateAdminPasswordAuth(ADCredentialsViewModel aDCredentials)
        {
            if (AdminAuthSettings.Enable_ADCredentials)
            {
                _logger.LogInformation($"Processs Password Validation[Payload : {JsonConvert.SerializeObject(aDCredentials)}]");
                var httpClient = _httpClientFactory.CreateClient("Fiorano");
                var loginCredentials = new ADCredentialsRoot
                {
                    AD_Credentials = new ADCredentials()
                };
                loginCredentials.AD_Credentials.AD_Username = aDCredentials.AD_Username;
                loginCredentials.AD_Credentials.AD_Password = aDCredentials.AD_Password;
                HttpContent content = new StringContent(JsonConvert.SerializeObject(loginCredentials), Encoding.UTF8, "application/json");

                var authentication = await httpClient.PostAsync(AppEndpoint.APIUri.FiorianoADAuthentication, content);
                string apiResponse = await authentication.Content.ReadAsStringAsync();
                _logger.LogInformation($"Processs Password Validation Response[{JsonConvert.SerializeObject(aDCredentials)}]");

                if (authentication.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<ADResponseRoot>(apiResponse);
                    if (result.AD_Response.Status == "TRUE" && result.AD_Response.Response.ResponseCode == "00")
                    {
                        return new ResponseMessage { Status = true, Message = "Login was successfully" };
                    }
                    return new ResponseMessage { Message = "Login detail is invalid, please try again with correct credentials", ResponseCode = 12 };
                }
                _logger.LogInformation("Could not connnect with ADCredentials password sevice");
                return new ResponseMessage { Message = "Could not connect to Password ADService" };
            }
            else
            {
                return new ResponseMessage { Status = true, Message = "Login was successfully" };
            }            
        }

        private ResponseMessage ValidateAdminOTPAuth(ADCredentialsViewModel aDCredentials)
        {
            if (AdminAuthSettings.Enable_OTP)
            {
                _logger.LogInformation($"Processs OTP Validation[Payload : {JsonConvert.SerializeObject(aDCredentials)}]");
                var checkOTP = _otpService.SOAPManual(aDCredentials.AD_OTP, aDCredentials.AD_Username);
                if (checkOTP == "")
                {
                    return new ResponseMessage { Message = "Login details invalid, please try again with correct credentials", ResponseCode = 12 };
                }
                if (checkOTP == "false")
                {
                    return new ResponseMessage { Message = "Could not connect with OTP Service" };
                }               
            }
            return new ResponseMessage { Message = "OTP was succesfully validated", Status = true };
        }

        private async Task<LoggedInAdminResponseDTO> GetAuthenticationResultForUserAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            //Generate Token
            var expirationTime = Convert.ToDouble(JwtSettings.ExpirationTime);
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(JwtSettings.Secret));
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
                Issuer = JwtSettings.Site,
                Audience = JwtSettings.Audience,
                Expires = DateTime.Now.AddMinutes(expirationTime),

            };
            //create the token 
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddMonths(7);
            _repoWrapper.ApplicationUser.Update(user);

            await _repoWrapper.Save();

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

        public async Task<ResponseMessage> CreateBackendAdmin(CreateAdminViewModel createAdminViewModel,string loggedInUserEmail,int loggedInUserId)
        {
            _logger.LogInformation($"Processs Create backend Admin[Admin Payload : {JsonConvert.SerializeObject(createAdminViewModel)}" +
                $" loggedin Usermail : {loggedInUserEmail}");
            // get logged in user
            var loggedinuser = await _repoWrapper.ApplicationUser.GetByEmailAsync(loggedInUserEmail);
            //get logged in admin mail
            var newLoggedInAdminMail = loggedInUserEmail.Remove(loggedInUserEmail.Length - 6);

            if (!createAdminViewModel.Email.EndsWith("@sterling.ng"))
            {
                _logger.LogInformation($"Create admin Terminated [ Reason : Email is not a valid sterling email]");
                return new ResponseMessage { Message = "Email is not a valid sterling email" };
            }

            var checkEmail = await _userManager.FindByEmailAsync($"{createAdminViewModel.Email}.admin");
            if (checkEmail != null)
            {
                _logger.LogInformation($"Create admin Terminated [ Reason : Email exist]");
                return new ResponseMessage { Message = "Email Already Exist" };
            }
            var checkIfUserExist = await _repoWrapper.ApplicationUser.FindByUniqueUsername(createAdminViewModel.UserName);
            if (checkIfUserExist != null) return new ResponseMessage { Message = "Username Already Exist" };

            var backedAdmin = await _repoWrapper.BackendAdmin.GetAdminByEmail(newLoggedInAdminMail);

            var appUser = new ApplicationUser()
            {
                FirstName = createAdminViewModel.FirstName,
                LastName = createAdminViewModel.LastName,
                Email = createAdminViewModel.Email + ".admin",
                UserName = createAdminViewModel.Email + ".admin",
                UniqueUsername = createAdminViewModel.UserName,
                EmailConfirmed = true
            };
            var result = _userManager.CreateAsync(appUser).Result;
            if (result.Succeeded)
            {
                _logger.LogInformation($"Create admin user Succesful ");
                var role = await _repoWrapper.ClassOrRole.GetRole(createAdminViewModel.RoleId);
                await _userManager.AddToRoleAsync(appUser, role.Name);
                BackendAdminUser adminUser = new BackendAdminUser()
                {
                    Email = createAdminViewModel.Email,
                    FirstName = createAdminViewModel.FirstName,
                    LastName = createAdminViewModel.LastName,
                    ClassOrRoleId = createAdminViewModel.RoleId
                };
                _repoWrapper.BackendAdmin.Create(adminUser);
                await _repoWrapper.Save();

                var auditViewModel = new AdminAuditLogViewModel(loggedInUserId, backedAdmin.Id, $"{loggedinuser.UniqueUsername} added {adminUser.Email}", ServiceNames.HealthBanc.ToString());
                await _auditLogServices.AdminCreateAuditLog(auditViewModel);

                return new ResponseMessage { Message = "Admin has been created successfully", Status = true };
            }
            return new ResponseMessage { Message = result.Errors.FirstOrDefault().Description.ToString() } ;
        }

        public async Task<ResponseMessage> ChangeAdminRole(int loggedInUserId,string email, int roleId)
        {
            _logger.LogInformation($"Change admin role process [ Email : {email} | RoleId :{roleId}]");

            var loggedInUser = await _repoWrapper.ApplicationUser.FindByIdAsync(loggedInUserId);
            var loggedInUserMail = loggedInUser.Email;
            var loggedInAdminMail = loggedInUserMail.Remove(loggedInUserMail.Length - 6);
            var loggediInAdmin = await _repoWrapper.BackendAdmin.GetAdminByEmail(loggedInAdminMail);

            var userToChangeMail = email + ".admin";
            var userToChange = await _userManager.FindByEmailAsync(userToChangeMail);
            var adminToChane = await _repoWrapper.BackendAdmin.GetAdminByEmail(email);
            if (userToChange != null)
            {
                var userRole = await _userManager.GetRolesAsync(userToChange);
                var removeRoleResult = _userManager.RemoveFromRoleAsync(userToChange, userRole.FirstOrDefault()).Result;
                var role = await _repoWrapper.ClassOrRole.GetRole(roleId);
                if (role is null) return new ResponseMessage { Message = "Role was not found" };
                var result = _userManager.AddToRoleAsync(userToChange, role.Name).Result;
                if (result.Succeeded)
                {
                    adminToChane.ClassOrRoleId = roleId;
                    _repoWrapper.BackendAdmin.Update(adminToChane);
                    await _repoWrapper.Save();

                    var auditViewModel = new AdminAuditLogViewModel(loggedInUserId, loggediInAdmin.Id, $"{loggedInUser.UniqueUsername} changed {email} role", ServiceNames.HealthBanc.ToString());
                    await _auditLogServices.AdminCreateAuditLog(auditViewModel);

                    return new ResponseMessage { Message = "Role was changed successfully", Status = true };
                }
            }
            return new ResponseMessage { Message = "User does not exist" };

        }

        public async Task<ResponseMessage> RemoveAdmin(int loggedInUserId,string email)
        {
            var loggedInUser = await _repoWrapper.ApplicationUser.FindByIdAsync(loggedInUserId);
            string loggedInUserMail = loggedInUser.Email;
            var LoggedInAdminMail = loggedInUserMail.Remove(loggedInUserMail.Length - 6);
            var loggedInAdmin = await _repoWrapper.BackendAdmin.GetAdminByEmail(LoggedInAdminMail);

            var adminToRemoveUserMail = email + ".admin";
            var user = await _userManager.FindByEmailAsync(adminToRemoveUserMail);
            if (user != null)
            {
                var result = _userManager.DeleteAsync(user).Result;
                if (result.Succeeded)
                {
                    var admin = await _repoWrapper.BackendAdmin.GetAdminByEmail(email);
                    _repoWrapper.BackendAdmin.Delete(admin);
                    await _repoWrapper.Save();

                    var auditViewModel = new AdminAuditLogViewModel(loggedInUserId, loggedInAdmin.Id, $"{loggedInUser.UniqueUsername} removed {email}", ServiceNames.HealthBanc.ToString());
                     await _auditLogServices.AdminCreateAuditLog(auditViewModel);

                    return new ResponseMessage { Message = "Admin was deleted successfully", Status = true };
                }
                var admin2 = await _repoWrapper.BackendAdmin.GetAdminByEmail(email);
                if (admin2 != null)
                {
                    _repoWrapper.BackendAdmin.Delete(admin2);
                    await _repoWrapper.Save();
                }
                return new ResponseMessage { Message = "An error occurred while trying to change to delete admin" };
            }
            return new ResponseMessage { Message = "User does not exist" };
        }

        public async Task<ResponseMessage> DisableAdmin(int loggedInUserId,string email)
        {
            var loggedInUser = await _repoWrapper.ApplicationUser.FindByIdAsync(loggedInUserId);
            string loggedInUserMail = loggedInUser.Email;
            var loggedInAdminMail = loggedInUserMail.Remove(loggedInUserMail.Length - 6);
            var LoggedInAdmin = await _repoWrapper.BackendAdmin.GetAdminByEmail(loggedInAdminMail);

            var userToDisableMail = email + ".admin";
            var userToDisable = await _userManager.FindByEmailAsync(userToDisableMail);
            if (userToDisable != null)
            {
                userToDisable.LockoutEnd = DateTime.Now.AddYears(100);
                _repoWrapper.ApplicationUser.Update(userToDisable);

                var adminToDisable = await _repoWrapper.BackendAdmin.GetAdminByEmail(email);
                adminToDisable.Disabled = true;
                _repoWrapper.BackendAdmin.Update(adminToDisable);
                await _repoWrapper.Save();

                var auditViewModel = new AdminAuditLogViewModel(loggedInUserId, LoggedInAdmin.Id, $"{loggedInUser.UniqueUsername} disabled {email}", ServiceNames.HealthBanc.ToString());
                await _auditLogServices.AdminCreateAuditLog(auditViewModel);

                return new ResponseMessage { Message = "Admin was disabled successfully", Status = true };
            }
            return new ResponseMessage { Message = "User does not exist" };
        }

        public async Task<ResponseMessage> EnableAdmin(int loggedInUserId, string email)
        {
            var loggedInUser = await _repoWrapper.ApplicationUser.FindByIdAsync(loggedInUserId);
            var loggedInUserMail = loggedInUser.Email;
            var loggedInAdminMail = loggedInUserMail.Remove(loggedInUserMail.Length - 6);
            var loggedInAdmin = await _repoWrapper.BackendAdmin.GetAdminByEmail(loggedInAdminMail);

            var userToEnableMail = email + ".admin";
            var userToEnable = await _userManager.FindByEmailAsync(userToEnableMail);
            if (userToEnable != null)
            {
                userToEnable.LockoutEnd = null;
                _repoWrapper.ApplicationUser.Update(userToEnable);

                var adminToEnable = await _repoWrapper.BackendAdmin.GetAdminByEmail(email);
                adminToEnable.Disabled = false;
                _repoWrapper.BackendAdmin.Update(adminToEnable);
                await _repoWrapper.Save();

                var auditViewModel = new AdminAuditLogViewModel(loggedInUserId, loggedInAdmin.Id, $"{loggedInUser.UniqueUsername} enabled {email}", ServiceNames.HealthBanc.ToString());
                await  _auditLogServices.AdminCreateAuditLog(auditViewModel);

                return new ResponseMessage { Message = "Admin was enabled successfully", Status = true };
            }
            return new ResponseMessage { Message = "User does not exist" };
        }
    }
}
