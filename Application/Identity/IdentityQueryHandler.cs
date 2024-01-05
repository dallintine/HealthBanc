using Application.Common.DTO;
using Application.Common.Interfaces;
using Application.Identity.Queries;
using Domain.Entities;
using Domain.Enums;
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
    public class IdentityQueryHandler : IRequestHandler<LoginQuery, BaseResponse>,
        IRequestHandler<LogOutQuery, BaseResponse>,
        IRequestHandler<ResendConfirmationOTPQuery,BaseResponse>,
        IRequestHandler<RefreshTokenQuery,BaseResponse>

    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<IdentityQueryHandler> _logger;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly IWebHostEnvironment _environment;
        private readonly IEmailService _emailService;
        private readonly IOTPService _otpService;
        private readonly IEncryptionService _encryption;

        public IdentityQueryHandler(UserManager<ApplicationUser> userManager, ILogger<IdentityQueryHandler> logger, SignInManager<ApplicationUser> signInManager
            , ITokenService tokenService, IWebHostEnvironment environment, IEmailService emailService, IOTPService otpService,IEncryptionService encryption)
        {
            _userManager = userManager;
            _logger = logger;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _environment = environment;
            _emailService = emailService;
            _otpService = otpService;
            _encryption = encryption;
        }

        public async Task<BaseResponse> Handle(LoginQuery request, CancellationToken cancellationToken)
        {
            var decryptedEmail = _encryption.DecryptString(request.Email);
            if (!decryptedEmail.Item1)
            {
                return BaseResponse.Failure("30", "Email is not encrypted");
            }
            var decryptedPassword = _encryption.DecryptString(request.Password);
            if (!decryptedPassword.Item1)
            {
                return BaseResponse.Failure("30", "Password is not encrypted");
            }
            var user = await _userManager.FindByEmailAsync(decryptedEmail.Item2);
            if (user is null)
            {
                _logger.LogInformation($"Login Terminated [Reason : Email not found | Email :  {decryptedEmail.Item2}]");
                return BaseResponse.Failure("12", "Email or password do not match for an existing user");
            }
            if (user.OAuthSubject != OAuthSubject.HealthBanc.ToString())
            {
                return BaseResponse.Failure("07", "Kindly Sigin with original auth method");
            }
            var passwordCheck = await _signInManager.PasswordSignInAsync(user, decryptedPassword.Item2, false, true);
            if (passwordCheck.IsLockedOut)
            {
                _logger.LogInformation($"Login terminated [Reason : User is locked out | Email : {decryptedEmail.Item2}] \n");
                return BaseResponse.Failure("12", " Your account is locked please try again with correct email and/or password in 60 minutes");
            }
            if (!passwordCheck.Succeeded)
            {
                _logger.LogInformation($"Login terminated [Reason : Password Check Failed | Email : {decryptedEmail.Item2}] \n");
                return BaseResponse.Failure("12", "Email or password do not match for an existing user");
            }
            if (!user.EmailConfirmed)
            {
                return BaseResponse.Failure("06", "Kindly enter OTP");
            }

            return await _tokenService.GetAuthenticationResultForUserAsync(user);
        }

        public async Task<BaseResponse> Handle(LogOutQuery request, CancellationToken cancellationToken)
        {
            await _tokenService.ClearSession();
            return BaseResponse.Success();
        }

        public async Task<BaseResponse> Handle(ResendConfirmationOTPQuery request, CancellationToken cancellationToken)
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
                var path = Path.Combine(_environment.WebRootPath, "EmailTemplates") + "/genericTemplate.html";
                var htmlTemplate = File.ReadAllText(path);
                var emailTemplate = htmlTemplate.Replace("{{Name}}", user.FirstName).Replace("{{Content}}", message);
                await _emailService.EmailRequest(new EmailRequest
                {
                    Subject = "Confirm HealthBanc Account",
                    Message = emailTemplate,
                    Email = user.Email
                });
                return BaseResponse.Success("OTP sent successfully");
            }
            return BaseResponse.Failure("25", "Username Does Not Exist");
        }

        public async Task<BaseResponse> Handle(RefreshTokenQuery request, CancellationToken cancellationToken)
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
