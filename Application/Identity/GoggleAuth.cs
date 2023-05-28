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
    public class GoggleAuth
    {
        public class Command : IRequest<BaseResponse>
        {
            public string Email { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Email).NotEmpty().NotNull().EmailAddress();
            }
        }

        public class Handler : IRequestHandler<Command, BaseResponse>
        {
            private readonly ILogger<Handler> _logger;
            private readonly UserManager<ApplicationUser> _userManager;
            private readonly IWebHostEnvironment _environment;
            private readonly IEmailService _emailService;
            private readonly ITokenService _tokenService;

            public Handler(ILogger<Handler> logger, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment
                , IEmailService emailService,ITokenService tokenService)
            {
                _logger = logger;
                _userManager = userManager;
                _environment = environment;
                _emailService = emailService;
                _tokenService = tokenService;
            }
            public async Task<BaseResponse> Handle(Command request, CancellationToken cancellationToken)
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if ( user is null)
                {
                    user = new ApplicationUser
                    {
                        UserName = request.Email,
                        Email = request.Email,
                        FirstName = request.FirstName,
                        LastName = request.LastName,
                        OAuthSubject = OAuthSubject.Goggle.ToString()
                    };

                    var result = await _userManager.CreateAsync(user);
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, Roles.User.ToString());
                        await _userManager.UpdateAsync(user);
                    }
                    else
                    {
                        string error = result.Errors.FirstOrDefault().Description;
                        _logger.LogInformation($"Register user terminated [Reason : Creating user failed | Error : {error}]\n");
                        return BaseResponse.Failure("06", error);
                    }
                }
                return await _tokenService.GetAuthenticationResultForUserAsync(user);
            }
        }
    }
}
