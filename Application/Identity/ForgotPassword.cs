using Application.CommonDTO;
using Application.Common.DTO;
using Application.Common.ConfigSettings;
using Application.Common.Interfaces;
using Domain.Entities;
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
using System.Web;

namespace Application.Identity
{
    public class ForgotPassword
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
            private readonly ITokenService _tokenService;
            private readonly IWebHostEnvironment _environment;
            private readonly IEmailService _emailService;
            private readonly AppEndpointSettings _appEndpointSettings;

            public Handler(UserManager<ApplicationUser> userManager, ILogger<Handler> logger, ITokenService tokenService, IOptions<AppEndpointSettings> appEndpointSettings,
                IWebHostEnvironment environment,IEmailService emailService)
            {
                _userManager = userManager;
                _logger = logger;
                _tokenService = tokenService;
                _environment = environment;
                _emailService = emailService;
                _appEndpointSettings = appEndpointSettings.Value;
            }
            public async Task<BaseResponse> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _userManager.FindByNameAsync(request.Email);
                if (user != null)
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var email = user.UserName;
                    string passwordResetLink = $"{_appEndpointSettings.FrontendBaseUrl}{_appEndpointSettings.ResetPassword}?email={HttpUtility.UrlEncode(email)}&emailToken={HttpUtility.UrlEncode(token)}";
                    var path = Path.Combine(_environment.WebRootPath, "EmailTemplates") + "\\resetPassword.html";
                    var htmlTemplate = File.ReadAllText(path);
                    var resetPasswordTemplate = htmlTemplate.Replace("{{Name}}", user.FirstName).Replace("{{BaseUrl}}", _appEndpointSettings.FrontendBaseUrl)
                        .Replace("{{ResetLink}}", passwordResetLink);
                    await _emailService.EmailRequest(new EmailRequest
                    {
                        Subject = "Forgot Password",
                        Message = resetPasswordTemplate,
                        Email = user.Email
                    });
                    return BaseResponse.Success();
                }
                return BaseResponse.Failure("25", "Username Does Not Exist");
            }
        }
    }
}
