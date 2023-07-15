using Application.Common.ConfigSettings;
using Application.Common.DTO;
using Application.Common.Interfaces;
using Application.CommonDTO;
using Application.Identity.Commands;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Application.Identity
{
    public class IdentityCommandHandler : IRequestHandler<ForgotPasswordCommand, BaseResponse>,
    IRequestHandler<ConfirmEmailCommand, BaseResponse>,
    IRequestHandler<ChangePasswordCommand, BaseResponse>,
    IRequestHandler<RegisterCommand, BaseResponse>,
    IRequestHandler<ResetPasswordCommand , BaseResponse>,
        IRequestHandler<GoggleAuthCommand, BaseResponse>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<IdentityCommandHandler> _logger;
        private readonly ITokenService _tokenService;
        private readonly IWebHostEnvironment _environment;
        private readonly IEmailService _emailService;
        private readonly ApplicationDbContext _context;
        private readonly IOTPService _otpService;
        private readonly AppEndpointSettings _appEndpointSettings;

        public IdentityCommandHandler(UserManager<ApplicationUser> userManager, ILogger<IdentityCommandHandler> logger, ITokenService tokenService, IOptions<AppEndpointSettings> appEndpointSettings,
            IWebHostEnvironment environment, IEmailService emailService, ApplicationDbContext context,IOTPService otpService)
        {
            _userManager = userManager;
            _logger = logger;
            _tokenService = tokenService;
            _environment = environment;
            _emailService = emailService;
            _context = context;
            _otpService = otpService;
            _appEndpointSettings = appEndpointSettings.Value;
        }

        public async Task<BaseResponse> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
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

        public async Task<BaseResponse> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Confirm Account [Email : {request.Email} ]\n");

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null) BaseResponse.Failure("25", "User records not found");

            var confirmOTP = await _otpService.ValidateOTP(user.Id, request.OTP, OTPActions.ConfirmAccount.ToString());
            if (confirmOTP.Code != "00")
            {
                _logger.LogInformation($"Confirm Account Terminate [Reason : OTP is invalid | Email : {request.Email} | UserId : {user.Id} ]\n");
                return BaseResponse.Failure(confirmOTP.Code, confirmOTP.Description);
            }

            _logger.LogInformation($"User was confirmed successfully [Email :{request.Email}] \n");
            user.EmailConfirmed = true;
            user.PhoneNumberConfirmed = true;
            await _userManager.UpdateAsync(user);
            await _context.SaveChangesAsync();
            return BaseResponse.Success("Account successfully created. Login to continue");
        }

        public async Task<BaseResponse> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            if (request.NewPassword == request.CurrentPassword)
            {
                return BaseResponse.Failure("30", "Current password cannot be the same with new password");
            }
            var userId = _tokenService.GetClaims().FirstOrDefault(x => x.Type == "UserId")?.Value;
            _logger.LogInformation($"Change Password [UserID :{userId}]\n");
            var user = await _context.Users.FindAsync(long.Parse(userId));
            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (result.Succeeded)
            {
                await _userManager.ResetAccessFailedCountAsync(user);
                return BaseResponse.Success("Password was changed successfully");
            }
            return BaseResponse.Failure("12", result.Errors.FirstOrDefault().Description);
        }

        public async Task<BaseResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
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
                    PhoneNumber = request.PhoneNumber,
                    OAuthSubject = OAuthSubject.HealthBanc.ToString()
                };

                var result = await _userManager.CreateAsync(user, request.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, Roles.User.ToString());
                    await _userManager.UpdateAsync(user);

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
                    return BaseResponse.Success();
                }
                else
                {
                    string error = result.Errors.FirstOrDefault().Description;
                    _logger.LogInformation($"Register user terminated [Reason : Creating user failed | Error : {error}]\n");
                    return BaseResponse.Failure("06", error);
                }
            }
            _logger.LogInformation($"Register user terminated [Reason : Email/Phonnumber is not unique]\n");
            return BaseResponse.Failure("26", "Email already exist");
        }

        public async Task<BaseResponse> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
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
                return BaseResponse.Failure("12", "Invalid Token");

            }
            return BaseResponse.Failure("12", userPassword.Errors.FirstOrDefault().Description);
        }

        public async Task<BaseResponse> Handle(GoggleAuthCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null)
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
            if (user.OAuthSubject == OAuthSubject.Goggle.ToString())
            {
                return await _tokenService.GetAuthenticationResultForUserAsync(user);
            }
            else
            {
                return BaseResponse.Failure("07", "Kindly Sigin with original auth method");
            }
        }
    }
}
