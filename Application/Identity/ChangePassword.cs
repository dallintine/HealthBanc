using Application.CommonDTO;
using Application.Interfaces;
using DataAccess;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Application.Identity
{
    public class ChangePassword
    {
        public class Command : IRequest<BaseResponse>
        {
            public string CurrentPassword { get; set; }
            public string NewPassword { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.CurrentPassword).NotEmpty().NotNull();
                RuleFor(x => x.NewPassword).NotEmpty().NotNull();
            }
        }

        public class Handler : IRequestHandler<Command, BaseResponse>
        {
            private readonly ILogger<Handler> _logger;
            private readonly UserManager<ApplicationUser> _userManager;
            private readonly ITokenService _tokenService;
            private readonly IRepositoryWrapper _repositoryWrapper;

            public Handler(ILogger<Handler> logger, UserManager<ApplicationUser> userManager,ITokenService tokenService, IRepositoryWrapper repositoryWrapper)
            {
                _logger = logger;
                _userManager = userManager;
                _tokenService = tokenService;
                _repositoryWrapper = repositoryWrapper;
            }

            public async Task<BaseResponse> Handle(Command request, CancellationToken cancellationToken)
            {
                if (request.NewPassword == request.CurrentPassword)
                {
                    return BaseResponse.Failure("30", "Current password cannot be the same with new password");
                }
                var userId = _tokenService.GetClaims().FirstOrDefault(x => x.Type == "UserId")?.Value;
                _logger.LogInformation($"Change Password [UserID :{userId}]\n");
                var user = await _repositoryWrapper.ApplicationUser.Find(x => x.Id == long.Parse(userId));
                var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
                if (result.Succeeded)
                {
                    await _userManager.ResetAccessFailedCountAsync(user);
                    return BaseResponse.Success("Password was changed successfully");
                }
                return BaseResponse.Failure("12", result.Errors.FirstOrDefault().Description);
            }
        }
    }
}
