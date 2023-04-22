using Application.CommonDTO;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Identity
{
    public class Register
    {
        public class Command : IRequest<BaseResponse>
        {
            public string Email { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Password { get; set; }
            public string PhoneNumber { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Email).NotEmpty().NotNull().EmailAddress();
                RuleFor(x => x.FirstName).NotEmpty().NotNull();
                RuleFor(x => x.LastName).NotEmpty().NotNull();
                RuleFor(x => x.Password).NotEmpty().NotNull();
                RuleFor(x => x.PhoneNumber).NotEmpty().NotNull();
            }
        }

        public class Handler : IRequestHandler<Command, BaseResponse>
        {
            private readonly ILogger<Handler> _logger;
            private readonly UserManager<ApplicationUser> _userManager;
            private readonly IWebHostEnvironment _environment;
            private readonly IOTPService _otpService;
            private readonly IEmailService _notificationService;

            public Handler(ILogger<Handler> logger,UserManager<ApplicationUser> userManager, IWebHostEnvironment environment,IOTPService otpService
                , IEmailService notificationService)
            {
                _logger = logger;
                _userManager = userManager;
                _environment = environment;
                _otpService = otpService;
                _notificationService = notificationService;
            }
            public async Task<BaseResponse> Handle(Command request, CancellationToken cancellationToken)
            {
                var email = await _userManager.FindByEmailAsync(request.Email);
                if (email is null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = request.Email,
                        Email = request.Email,
                        FirstName = request.FirstName,
                        LastName = request.LastName,
                        PhoneNumber = request.PhoneNumber
                    };

                    var result = await _userManager.CreateAsync(user, request.Password);
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, "role");
                        await _userManager.UpdateAsync(user);

                        var createOTP = await _otpService.CreateOTP(user.Id, OTPActions.ConfirmAccount.ToString());
                        var otpCode = createOTP.Data;

                        var message = $"This is your OTP number {otpCode}. Use it to confirm your account";
                        var path = Path.Combine(_environment.WebRootPath, "EmailTemplates") + "\\genericTemplate.html";
                        var htmlTemplate = File.ReadAllText(path);
                        var emailTemplate = htmlTemplate.Replace("{{Name}}", user.FirstName).Replace("{{Content}}", message);
                        await _notificationService.Email(new EmailNotificationModel
                        {
                            Subject = "Confirm SMECollect Account",
                            Body = emailTemplate,
                            To = new List<string> { user.Email }
                        });
                        return BaseResponse.Success();
                    }
                    else
                    {
                        string error = result.Errors.FirstOrDefault().Description;
                        _logger.LogInformation($"Register user terminated [Reason : Creating user failed | Error : {error}]\n");
                        return BaseResponse.Failure("06",error);
                    }
                }
                _logger.LogInformation($"Register user terminated [Reason : Email/Phonnumber is not unique]\n");
                return BaseResponse.Failure("26","Email already exist");
            }
        }
    }
}
