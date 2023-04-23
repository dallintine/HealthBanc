using Application.CommonDTO;
using Application.Identity;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ITokenService
    {
        Task<BaseResponse> ClearSession(long userId);
        Task<BaseResponse> GetAuthenticationResultForUserAsync(ApplicationUser user);
        List<Claim> GetClaims();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }
}
