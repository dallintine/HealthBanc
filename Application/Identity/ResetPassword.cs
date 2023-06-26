using Application.Common.DTO;
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
    public class ResetPassword
    {
        public class Command : IRequest<BaseResponse>
        {
            public string Email { get; set; }
            public string EmailToken { get; set; }
            public string Password { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Email).NotEmpty().NotNull();
                RuleFor(x => x.EmailToken).NotEmpty().NotNull();
                RuleFor(x => x.Password).NotEmpty().NotNull();
            }
        }

        public class Handler : IRequestHandler<Command, BaseResponse>
        {
            private readonly ILogger<Handler> _logger;
            private readonly UserManager<ApplicationUser> _userManager;

            public Handler(ILogger<Handler> logger, UserManager<ApplicationUser> userManager)
            {
                _logger = logger;
                _userManager = userManager;
            }
            public async Task<BaseResponse> Handle(Command request, CancellationToken cancellationToken)
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user is null)
                {
                    _logger.LogInformation($"ResetPassword terminated [Reason : User not found | Email : {request.Email}] \n");
                    return BaseResponse.Failure("25", "User record not found");
                }

                request.EmailToken = request.EmailToken.Replace(" ", "+");
                var userPassword = await _userManager.ResetPasswordAsync(user, request.EmailToken, request.Password);
                if (userPassword.Succeeded)
                {
                    user.EmailConfirmed = true;
                    user.LockoutEnd = null;
                    await _userManager.ResetAccessFailedCountAsync(user);
                    await _userManager.UpdateAsync(user);
                    return BaseResponse.Success();
                }
                else if (!userPassword.Succeeded && userPassword.Errors.Any(x => x.Code == "InvalidToken"))
                {
                    _logger.LogInformation($"Reset Password terminated [ Reason :Invalid Token | Email : {request.Email}] \n");
                    return BaseResponse.Failure( "12","Invalid Token");

                }
                return BaseResponse.Failure("12", userPassword.Errors.FirstOrDefault().Description);
            }
        }
    }
}
