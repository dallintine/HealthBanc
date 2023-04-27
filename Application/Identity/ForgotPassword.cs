using Application.CommonDTO;
using Application.Core.ConfigSettings;
using Application.Interfaces;
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
            private readonly AppEndpointSettings _appEndpointSettings;

            public Handler(UserManager<ApplicationUser> userManager, ILogger<Handler> logger, ITokenService tokenService, IOptions<AppEndpointSettings> appEndpointSettings,
                IWebHostEnvironment environment)
            {
                _userManager = userManager;
                _logger = logger;
                _tokenService = tokenService;
                _environment = environment;
                _appEndpointSettings = appEndpointSettings.Value;
            }
            public async Task<BaseResponse> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _userManager.FindByNameAsync(request.Email);
                if (user != null)
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var email = user.UserName;
                    string passwordResetLink = $"{_appEndpointSettings.FrontendBaseUrl}?email={HttpUtility.UrlEncode(email)}&emailToken={HttpUtility.UrlEncode(token)}";

                    //await _emailSender.SendOneDrugStoreUserResetPasswordMail(forgotPassword.Username, "Reset your password", passwordResetLink);

                    return BaseResponse.Success();
                }
                return BaseResponse.Failure("25", "Username Does Not Exist");
            }
        }
    }
}
