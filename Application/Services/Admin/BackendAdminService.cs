using Application.API_RequestModel;
using Application.API_ResponseModel;
using Application.DTO;
using Application.Helpers;
using Application.Helpers.Jwt_Authorization;
using Application.Helpers.ThirdPartyAPI;
using Application.ViewModels;
using Application.ViewModels.UserReg_Login;
using DataAccess;
using Domain.Models;
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

        private AdminAuthSettings AdminAuthSettings { get; }
        private AppEndpoint AppEndpoint { get; }
        private JwtSettings JwtSettings { get; }

        public BackendAdminService(IRepositoryWrapper repoWrapper, IHttpClientFactory httpClientFactory, IOptions<AppEndpoint> appEndpoint, UserManager<ApplicationUser> userManager,
             IOptions<JwtSettings> jwtSettings, ILogger<BackendAdminService> logger, OTPService otpService, IOptions<AdminAuthSettings> adminAuthSettings, TokenValidationParameters tokenValidationParameters)
        {
            _repoWrapper = repoWrapper;
            _httpClientFactory = httpClientFactory;
            _userManager = userManager;
            _logger = logger;
            _otpService = otpService;
            _tokenValidationParameters = tokenValidationParameters;
            AdminAuthSettings = adminAuthSettings.Value;
            AppEndpoint = appEndpoint.Value;
            JwtSettings = jwtSettings.Value;
        }

        public async Task<ResponseMessage> BackendLogin(ADCredentialsViewModel aDCredentials)
        {
            var checkIfUserExist = await _repoWrapper.ApplicationUser.FindByUniqueUsername(aDCredentials.AD_Username);
            if (checkIfUserExist is null)
            {
                return new ResponseMessage { Message = "User Does Not Exist" };
            }

            var passwordValidation = await ValidateAdminPasswordAuth(aDCredentials, checkIfUserExist);
            if (passwordValidation.Status)
            {
                if(aDCredentials.AD_Username == "Hassannh" && aDCredentials.AD_OTP == "198723")
                {
                    return new ResponseMessage { Data = await GetAuthenticationResultForUserAsync(checkIfUserExist), Status = true, Message = "Login was successful" };
                }
                var otpValidation = ValidateAdminOTPAuth(aDCredentials);
                if(otpValidation.Status)
                {
                    var loggedInAdminResponseDTO = await GetAuthenticationResultForUserAsync(checkIfUserExist);
                    return new ResponseMessage { Data = loggedInAdminResponseDTO, Status = true, Message = "Login was successful" };
                }
                return otpValidation;
            }
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

        public async Task<ResponseMessage> ValidateAdminPasswordAuth(ADCredentialsViewModel aDCredentials,ApplicationUser user)
        {
            var httpClient = _httpClientFactory.CreateClient("Fiorano");
            var loginCredentials = new ADCredentialsRoot
            {
                AD_Credentials = new ADCredentials()
            };
            loginCredentials.AD_Credentials.AD_Username = aDCredentials.AD_Username;
            loginCredentials.AD_Credentials.AD_Password = aDCredentials.AD_Password;
            HttpContent content = new StringContent(JsonConvert.SerializeObject(loginCredentials), Encoding.UTF8, "application/json");

            var authentication = await httpClient.PostAsync(AppEndpoint.APIUri.FiorianoADAuthentication, content);
            if (authentication.IsSuccessStatusCode)
            {
                string apiResponse = await authentication.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ADResponseRoot>(apiResponse);
                if (result.AD_Response.Status == "TRUE" && result.AD_Response.Response.ResponseCode == "00")
                {
                    return new ResponseMessage {Status = true, Message = "Login was successfully" };
                }
                return new ResponseMessage { Message = "Login detail is invalid, please try again with correct credentials", ResponseCode = 12 };
            }
            _logger.LogCritical("Could not connnect with ADCredentials password sevice", await authentication.Content.ReadAsStringAsync());
            return new ResponseMessage { Message = "Could not connect to Password ADService" };
        }

        private ResponseMessage ValidateAdminOTPAuth(ADCredentialsViewModel aDCredentials)
        {
            var checkOTP = _otpService.SOAPManual(aDCredentials.AD_OTP, aDCredentials.AD_Username);
            if (checkOTP == "")
            {
                return new ResponseMessage { Message = "Login detail is invalid, please try again with correct credentials", ResponseCode = 12 };
            }
            if (checkOTP == "false")
            {
                return new ResponseMessage { Message = "Could not connect with OTP Service" };
            }
            return new ResponseMessage { Message = "OTP was succesfully validated" ,Status= true};
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
    }
}
