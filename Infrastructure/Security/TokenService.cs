using Application.CommonDTO;
using Application.Core.ConfigSettings;
using Application.Identity;
using Application.Interfaces;
using Domain.Entities;
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
    public class TokenService : ITokenService
    {
        private readonly ILogger<TokenService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TokenValidationParameters _tokenValidation;
        private readonly JwtSettings _jwtSettings;

        public TokenService(ILogger<TokenService> logger, UserManager<ApplicationUser> userManager, IOptions<JwtSettings> jwtSettings, TokenValidationParameters tokenValidation)
        {
            _logger = logger;
            _userManager = userManager;
            _tokenValidation = tokenValidation;
            _jwtSettings = jwtSettings.Value;
        }

        public async Task<BaseResponse<LoginResponse>> GetAuthenticationResultForUserAsync(ApplicationUser user)
        {
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
            return BaseResponse<LoginResponse>.Success(logInResponse);
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
    }
}
