using Application.CommonDTO;
using Application.Interfaces;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Identity
{
    public class RefreshToken
    {
        public class Query : IRequest<BaseResponse>
        {
            public string Token { get; set; }
            public string RefreshToken { get; set; }
        }

        public class QueryValidator : AbstractValidator<Query>
        {
            public QueryValidator()
            {
                RuleFor(x => x.Token).NotEmpty().NotNull();
                RuleFor(x => x.RefreshToken).NotEmpty().NotNull();
            }
        }

        public class Handler : IRequestHandler<Query, BaseResponse>
        {
            private readonly UserManager<ApplicationUser> _userManager;
            private readonly ILogger<Handler> _logger;
            private readonly ITokenService _tokenService;

            public Handler(UserManager<ApplicationUser> userManager, ILogger<Handler> logger, ITokenService tokenService)
            {
                _userManager = userManager;
                _logger = logger;
                _tokenService = tokenService;
            }
            public async Task<BaseResponse> Handle(Query request, CancellationToken cancellationToken)
            {
                _logger.LogInformation($"Refresh Token Request processing \n");
                var principal = _tokenService.GetPrincipalFromExpiredToken(request.Token);
                var userId = principal.Identity.Name;
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogInformation($"Refresh Token terminated [Reason : User not found | Email : {user.Email}] \n");
                    return BaseResponse.Failure("25", "User could not found");
                }
                if (user.RefreshToken != request.RefreshToken)
                {
                    _logger.LogInformation($"Login terminated [Reason : Invalid Refresh Token | Email : {user.Email}] \n");
                    return BaseResponse.Failure("12", "Invalid refresh token");
                }
                if (user.RefreshTokenExpiryTime <= DateTime.Now)
                {
                    _logger.LogInformation($"Login terminated [Reason : Refresh Token has expired | Email : {user.Email}] \n");
                    return BaseResponse.Failure("12", "Invalid refresh token - token has expired");
                }

                return await _tokenService.GetAuthenticationResultForUserAsync(user);
            }
        }
    }
}
