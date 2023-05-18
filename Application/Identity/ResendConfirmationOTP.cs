using Application.CommonDTO;
using Application.Core.ConfigSettings;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Identity
{
    public class ResendConfirmationOTP
    {
        public class Query : IRequest<BaseResponse>
        {
            public string Email { get; set; }
        }

        public class QueryValidator : AbstractValidator<Query>
        {
            public QueryValidator()
            {
                RuleFor(x => x.Email).NotEmpty().NotNull().EmailAddress();
            }
        }

        public class Handler : IRequestHandler<Query, BaseResponse>
        {
            private readonly UserManager<ApplicationUser> _userManager;
            private readonly ILogger<Handler> _logger;
            private readonly IWebHostEnvironment _environment;
            private readonly IEmailService _emailService;
            private readonly IOTPService _otpService;

            public Handler(UserManager<ApplicationUser> userManager, ILogger<Handler> logger,
                IWebHostEnvironment environment, IEmailService emailService, IOTPService otpService)
            {
                _userManager = userManager;
                _logger = logger;
                _environment = environment;
                _emailService = emailService;
                _otpService = otpService;
            }
            public async Task<BaseResponse> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _userManager.FindByNameAsync(request.Email);
                if (user != null)
                {
                    if (user.EmailConfirmed)
                    {
                        return BaseResponse.Success("Account successfully created. Login to continue");
                    }
                    var createOTP = await _otpService.CreateOTP(user.Id, OTPActions.ConfirmAccount.ToString());
                    var otpCode = createOTP.Data;

                    var message = $"This is your OTP number {otpCode}. Use it to confirm your account";
                    var path = Path.Combine(_environment.WebRootPath, "EmailTemplates") + "\\genericTemplate.html";
                    var htmlTemplate = File.ReadAllText(path);
                    var emailTemplate = htmlTemplate.Replace("{{Name}}", user.FirstName).Replace("{{Content}}", message);
                    await _emailService.EmailRequest(new EmailRequest
                    {
                        Subject = "Confirm HealtBanc Account",
                        Message = emailTemplate,
                        Email = user.Email
                    });
                    return BaseResponse.Success("OTP sent successfully");
                }
                return BaseResponse.Failure("25", "Username Does Not Exist");
            }
        }
    }
}
