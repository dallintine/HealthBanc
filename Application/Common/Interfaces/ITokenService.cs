using Application.Common.DTO;
using Application.Identity;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Interfaces
{
    public interface ITokenService
    {
        Task<BaseResponse> ClearSession();
        Task<BaseResponse> GetAuthenticationResultForUserAsync(ApplicationUser user);
        List<Claim> GetClaims();
        string GetIP();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
        Task<BaseResponse> ValidateAdminPasswordAuth(string username, string password);
    }
}
