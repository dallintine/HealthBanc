using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc
{
    public class MyAuthorizationFilter : IDashboardAuthorizationFilter
    {
        private static readonly string HangFireCookieName = "HangFireCookie";
        private static readonly int CookieExpirationMinutes = 30;
        private TokenValidationParameters _tokenValidationParameters;
        private string _role;
        private readonly Serilog.ILogger _logger;

        public MyAuthorizationFilter(TokenValidationParameters tokenValidationParameters, Serilog.ILogger logger, string role = null)
        {
            _tokenValidationParameters = tokenValidationParameters;
            _role = role;
            _logger = logger;
        }

        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            var access_token = String.Empty;
            var setCookie = false;

            // try to get token from query string
            if (httpContext.Request.Query.ContainsKey("access_token"))
            {
                access_token = httpContext.Request.Query["access_token"].FirstOrDefault();
                setCookie = true;
            }
            else
            {
                access_token = httpContext.Request.Cookies[HangFireCookieName];
            }

            if (String.IsNullOrEmpty(access_token))
            {
                return false;
            }

            try
            {
                SecurityToken validatedToken = null;
                JwtSecurityTokenHandler hand = new JwtSecurityTokenHandler();
                var claims = hand.ValidateToken(access_token, _tokenValidationParameters, out validatedToken);
                if (!String.IsNullOrEmpty(_role) && !claims.IsInRole(_role))
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error during dashboard hangfire jwt validation process");
                return false;
            }

            if (setCookie)
            {
                httpContext.Response.Cookies.Append(HangFireCookieName,
                access_token,
                new CookieOptions()
                {
                    Expires = DateTime.Now.AddMinutes(CookieExpirationMinutes)
                });
            }

            return true;
        }
    }
}
