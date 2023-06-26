using Application.Common.DTO;
using Application.Common.Interfaces;
using DataAccess;
using Domain.Entities;
using Domain.Enums;
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
    public class ConfirmEmail
    {
        public class Command : IRequest<BaseResponse>
        {
            public string OTP { get; set; }
            public string Email { get; set; }

        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Email).NotEmpty().NotNull().EmailAddress();
                RuleFor(x => x.OTP).NotEmpty().NotNull();
            }
        }

        public class Handler : IRequestHandler<Command, BaseResponse>
        {
            private readonly ILogger<Handler> _logger;
            private readonly IRepositoryWrapper _repositoryWrapper;
            private readonly IOTPService _otpService;
            private readonly UserManager<ApplicationUser> _userManager;
            private readonly ITokenService _tokenService;

            public Handler(ILogger<Handler> logger, IRepositoryWrapper repositoryWrapper, IOTPService otpService , UserManager<ApplicationUser> userManager,ITokenService tokenService)
            {
                _logger = logger;
                _repositoryWrapper = repositoryWrapper;
                _otpService = otpService;
                _userManager = userManager;
                _tokenService = tokenService;
            }

            public async Task<BaseResponse> Handle(Command request, CancellationToken cancellationToken)
            {
                _logger.LogInformation($"Confirm Account [Email : {request.Email} ]\n");

                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user is null) BaseResponse.Failure("25", "User records not found" );

                var confirmOTP = await _otpService.ValidateOTP(user.Id, request.OTP, OTPActions.ConfirmAccount.ToString());
                if (confirmOTP.Code != "00")
                {
                    _logger.LogInformation($"Confirm Account Terminate [Reason : OTP is invalid | Email : {request.Email} | UserId : {user.Id} ]\n");
                    return BaseResponse.Failure( confirmOTP.Code, confirmOTP.Description );
                }

                _logger.LogInformation($"User was confirmed successfully [Email :{request.Email}] \n");
                user.EmailConfirmed = true;
                user.PhoneNumberConfirmed = true;
                await _userManager.UpdateAsync(user);
                await _repositoryWrapper.Save();
                return BaseResponse.Success("Account successfully created. Login to continue");
            }
        }
    }
}
