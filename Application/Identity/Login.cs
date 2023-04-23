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
    public class Login
    {
        public class Query : IRequest<BaseResponse<LoginResponse>>
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }

        public class QueryValidator : AbstractValidator<Query>
        {
            public QueryValidator()
            {
                RuleFor(x => x.Email).NotEmpty().NotNull().EmailAddress();
                RuleFor(x => x.Password).NotEmpty().NotNull();
            }
        }

        public class Handler : IRequestHandler<Query, BaseResponse>
        {
            private readonly UserManager<ApplicationUser> _userManager;
            private readonly ILogger<Handler> _logger;
            private readonly SignInManager<ApplicationUser> _signInManager;
            private readonly ITokenService _tokenService;

            public Handler(UserManager<ApplicationUser> userManager,ILogger<Handler> logger,SignInManager<ApplicationUser> signInManager,ITokenService tokenService)
            {
                _userManager = userManager;
                _logger = logger;
                _signInManager = signInManager;
                _tokenService = tokenService;
            }
            public async Task<BaseResponse> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user is null)
                {
                    _logger.LogInformation($"Login Terminated [Reason : Email not found | Email :  {request.Email}]");
                    return BaseResponse.Failure("12", "Login failed - Email or password do not match for an existing user");
                }
                var passwordCheck = await _signInManager.PasswordSignInAsync(user, request.Password, false, true);
                if (passwordCheck.IsLockedOut)
                {
                    _logger.LogInformation($"Login terminated [Reason : USer is locked out | Email : {request.Email}] \n");
                    return BaseResponse.Failure("12", "Login failed - Your account is locked please try again with correct email and/or password in 60 minutes");
                }
                if (!passwordCheck.Succeeded)
                {
                    _logger.LogInformation($"Login terminated [Reason : Password Check Failed | Email : {request.Email}] \n");
                    return BaseResponse.Failure("12", "Login failed - Email or password do not match for an existing user");
                }              

                return await _tokenService.GetAuthenticationResultForUserAsync(user);
            }
        }
    }
}
