using Application.CommonDTO;
using Application.Core.ConfigSettings;
using Application.Identity;
using Application.Interfaces;
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

namespace Infrastructure.Security
{
    public class TokenService : BaseService , ITokenService
    {
        private readonly ILogger<TokenService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TokenValidationParameters _tokenValidation;
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly JwtSettings _jwtSettings;

        public TokenService(ILogger<TokenService> logger, UserManager<ApplicationUser> userManager, IOptions<JwtSettings> jwtSettings, TokenValidationParameters tokenValidation,
            IRepositoryWrapper repositoryWrapper, IHttpContextAccessor accessor) : base(accessor)
        {
            _logger = logger;
            _userManager = userManager;
            _tokenValidation = tokenValidation;
            _repositoryWrapper = repositoryWrapper;
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

            var expiryTime = DateTimeOffset.Now.AddMinutes(7);
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

            var logInResponse = new LoginResponse
            {
                Token = tokenHandler.WriteToken(token),
                Username = user.Email,
                FullName = $"{user.FirstName} {user.LastName}",
                Roles = roles,
                ExpiryTime = expiryTime.LocalDateTime,
                RefreshToken = refreshToken
            };

            await _userManager.ResetAccessFailedCountAsync(user);
            await SaveSession(Device, IpAddress, user.Id, logInResponse.ExpiryTime);
            return BaseResponse<LoginResponse>.Success(logInResponse);
        }
        public async Task<BaseResponse> ClearSession(long userId)
        {
            var session = await _repositoryWrapper.UserSession.GetByUserId_Device(userId, IpAddress);
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
    }
}
