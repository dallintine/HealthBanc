using Application.Common.DTO;
using Application.Common.ConfigSettings;
using Application.Identity;
using Application.Common.Interfaces;
using DataAccess;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Application.AdminAuth.DTO;
using Infrastructure.Services;
using Application.Identity.DTO;

namespace Infrastructure.Security
{
    public class TokenService : BaseService , ITokenService
    {
        private readonly ILogger<TokenService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TokenValidationParameters _tokenValidation;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppEndpointSettings _appSettings;
        private readonly JwtSettings _jwtSettings;

        public TokenService(ILogger<TokenService> logger, UserManager<ApplicationUser> userManager, IOptions<JwtSettings> jwtSettings, TokenValidationParameters tokenValidation,
             IHttpClientFactory httpClientFactory, IOptions<AppEndpointSettings> appSettings , IHttpContextAccessor accessor) : base(accessor)
        {
            _logger = logger;
            _userManager = userManager;
            _tokenValidation = tokenValidation;
            _httpClientFactory = httpClientFactory;
            _appSettings = appSettings.Value;
            _jwtSettings = jwtSettings.Value;
        }

        public async Task<BaseResponse> GetAuthenticationResultForUserAsync(ApplicationUser user)
        {
            var checkSession = await UserInSession(user.Id, IpAddress, Device, false);

            if (checkSession.Code != "00")
            {
                return checkSession;
            }

            _logger.LogInformation($"Processing Auth Token For User [Email : {user.Email}] \n");
            var roles = await _userManager.GetRolesAsync(user);

            var expiryTime = DateTimeOffset.Now.AddMinutes(_jwtSettings.ExpirationTime);
            //Generate Token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtSettings.Secret));
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim("UserId",user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("FirstName",user.FirstName),
                    new Claim("LastName",user.LastName),
                    new Claim("LoggedOn", DateTime.Now.ToString())
                }),
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature),
                Issuer = _jwtSettings.Site,
                Audience = _jwtSettings.Audience,
                Expires = expiryTime.LocalDateTime,

            };
            tokenDescriptor.Subject.AddClaims(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            //create the token
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddMonths(5);
            user.LastLoginDate = DateTime.Now;
            await _userManager.UpdateAsync(user);

            var logInResponse = new LoginResponseDTO
            {
                Token = tokenHandler.WriteToken(token),
                Username = user.UserName,
                FullName = $"{user.FirstName} {user.LastName}",
                Roles = roles,
                ExpiryTime = expiryTime.LocalDateTime,
                RefreshToken = refreshToken
            };

            await _userManager.ResetAccessFailedCountAsync(user);
            await SaveSession(Device, IpAddress, user.Id, logInResponse.ExpiryTime);
            return BaseResponse<LoginResponseDTO>.Success(logInResponse);
        }
        public async Task<BaseResponse> ClearSession()
        {
            var session = await _repositoryWrapper.UserSession.GetByIp_Device(IpAddress, Device);
            if (session != null)
            {
                _repositoryWrapper.UserSession.Delete(session);
                await _repositoryWrapper.Save();
            }
            return BaseResponse.Success();
        }
        public List<Claim> GetClaims()
        {
            return Claims;
        }
        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, _tokenValidation, out SecurityToken securityToken);
            if (!(securityToken is JwtSecurityToken jwtSecurityToken) || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");
            return principal;
        }
        private string GenerateRefreshToken()
        {
            _logger.LogInformation($"Generating Refresh Token \n");
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
        private async Task<BaseResponse> UserInSession(long userId, string deviceIp, string browser, bool isAdmin)
        {
            var session = await _repositoryWrapper.UserSession.GetByUserId_Device(userId, deviceIp);
            if (session is null)
            {
                return BaseResponse.Success();
            }
            else
            {
                if (isAdmin && DateTime.Now < session.ExpiryDate && session.DeviceIp == deviceIp)
                {
                    return BaseResponse.Failure("12","You have an active session in another device. Sign out of it to Sign in here.");
                }
                else if (!isAdmin && DateTime.Now < session.ExpiryDate && session.DeviceIp == deviceIp && session.Browser.ToLower() == browser.ToLower())
                {
                    return BaseResponse.Failure("12",  "You have an active session in one of your browser!. Sign out of it to Sign in here." );
                }

                return BaseResponse.Success();
            }
        }
        private async Task SaveSession(string browser, string deviceIp, long userId, DateTime expiryTime)
        {
            var session = await _repositoryWrapper.UserSession.GetByUserId_Device(userId, deviceIp);
            if (session is null)
            {

                var newSession = new UserSession();
                newSession.Browser = browser;
                newSession.DeviceIp = deviceIp;
                newSession.UserId = userId;
                newSession.ExpiryDate = expiryTime;
                _repositoryWrapper.UserSession.Create(newSession);
            }
            else
            {
                session.Browser = browser;
                session.DeviceIp = deviceIp;
                session.ExpiryDate = expiryTime;
                _repositoryWrapper.UserSession.Update(session);
            }
            await _repositoryWrapper.Save();
        }

        public async Task<BaseResponse> ValidateAdminPasswordAuth(string username , string password)
        {
            var httpClient = _httpClientFactory.CreateClient("Fiorano");
            var loginCredentials = new ADCredentialsRoot
            {
                AD_Credentials = new ADCredentials()
            };
            loginCredentials.AD_Credentials.AD_Username = username;
            loginCredentials.AD_Credentials.AD_Password = password;
            HttpContent content = new StringContent(JsonConvert.SerializeObject(loginCredentials), Encoding.UTF8, "application/json");

            var authentication = await httpClient.PostAsync(_appSettings.FiorianoADAuthentication, content);
            string apiResponse = await authentication.Content.ReadAsStringAsync();
            if (authentication.IsSuccessStatusCode)
            {
                var result = JsonConvert.DeserializeObject<ADResponseRoot>(apiResponse);
                if (result.AD_Response.Status == "TRUE" && result.AD_Response.Response.ResponseCode == "00")
                {
                    return BaseResponse.Success();
                }
                return BaseResponse.Failure( "12" ,"Login detail is invalid, please try again with correct credentials");
            }
            _logger.LogInformation("Could not connnect with ADCredentials password sevice");
            return BaseResponse.Failure("06" , "Could not connect to Password ADService");
        }
    }
}
