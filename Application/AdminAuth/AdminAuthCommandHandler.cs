using Application.AdminAuth.Commands;
using Application.Common.ConfigSettings;
using Application.Common.DTO;
using Application.Common.Interfaces;
using DocumentFormat.OpenXml.Spreadsheet;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Application.AdminAuth;

public class AdminAuthCommandHandler : IRequestHandler<AdminLoginCommand, BaseResponse>,
IRequestHandler<CreateAdminCommand, BaseResponse>,
IRequestHandler<ResetPasswordCommand, BaseResponse>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminAuthCommandHandler> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IOTPService _otpService;
    private readonly IEncryptionService _encryption;
    private readonly IEmailService _emailService;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IWebHostEnvironment _environment;
    private readonly AppEndpointSettings _appEndpointSettings;
    private readonly DefaultAdmin _defaultAdminSettings;

    public AdminAuthCommandHandler(ApplicationDbContext context , ILogger<AdminAuthCommandHandler> logger, UserManager<ApplicationUser> userManager,ITokenService tokenService
        ,IOTPService otpService, IOptions<DefaultAdmin> defaultAdminSettings,IEncryptionService encryption, IEmailService emailService, SignInManager<ApplicationUser> signInManager,
        IOptions<AppEndpointSettings> appEndpointSettings, IWebHostEnvironment environment)
    {
        _context = context;
        _logger = logger;
        _userManager = userManager;
        _tokenService = tokenService;
        _otpService = otpService;
        _encryption = encryption;
        _emailService = emailService;
        _signInManager = signInManager;
        _environment = environment;
        _appEndpointSettings = appEndpointSettings.Value;
        _defaultAdminSettings = defaultAdminSettings.Value;
    }
    public async Task<BaseResponse> Handle(AdminLoginCommand request, CancellationToken cancellationToken)
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

        var admin = await _context.Users.SingleOrDefaultAsync(x => x.UserName == decryptedEmail.Item2, cancellationToken);
        if (admin is null)
        {
            _logger.LogInformation($"Login Terminated [Reason : Admin not found | Email :  {decryptedEmail.Item2}]");
            return BaseResponse.Failure("25", "Admin not found");
        }
        if (!admin.EmailConfirmed || admin.PasswordHash is null)
        {
            return BaseResponse.Failure("06", "Kindly set up your account password");
        }
        if (decryptedEmail.Item2 == _defaultAdminSettings.Email)
        {
            return await _tokenService.GetAuthenticationResultForUserAsync(admin);
        }
        if (_defaultAdminSettings.OTPValidation)
        {
            var otpValidation = await _otpService.ValidateAdminOTPAuth(request.OTP, admin.UniqueUsername);
            if (otpValidation.Code == "00")
            {
                var passwordValidation = await ValidatePassword(admin, decryptedPassword.Item2);
                if (passwordValidation.Code != "00")
                {
                    _logger.LogInformation($"Backend Login failed. Password [Reason : Password could not be validated]");
                    return passwordValidation;
                }
                return await _tokenService.GetAuthenticationResultForUserAsync(admin);
            }
            return otpValidation;
        }
        else
        {
            var passwordValidation = await ValidatePassword(admin, decryptedPassword.Item2);
            if (passwordValidation.Code != "00")
            {
                _logger.LogInformation($"Backend Login failed. Password [Reason : Password could not be validated]");
                return passwordValidation;
            }
            return await _tokenService.GetAuthenticationResultForUserAsync(admin);
        }
    }

    public async Task<BaseResponse> Handle(CreateAdminCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Create Admin Request [ Payload : {JsonConvert.SerializeObject(request)}] \n");
        var admin = await _context.Users.SingleOrDefaultAsync(x => x.UserName == request.Email,cancellationToken);
        if(admin != null)
        {
            return BaseResponse.Failure("26", "Admin with this email exist");
        }

        admin = new ApplicationUser()
        {
            UserName = request.Email,
            Email = $"{request.Email}.admin",
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = false,
            UniqueUsername = request.UniqueUsername
        };

        var result = _userManager.CreateAsync(admin).Result;

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(admin, Roles.Admin.ToString());
            _context.Users.Update(admin);
            await _context.SaveChangesAsync(cancellationToken);

            var token = await _userManager.GeneratePasswordResetTokenAsync(admin);
            string passwordResetLink = $"{_appEndpointSettings.FrontendAdminBaseUrl}{_appEndpointSettings.AdminChangePassword}?email={HttpUtility.UrlEncode(request.Email)}" +
                $"&emailToken={HttpUtility.UrlEncode(token)}";
            var path = Path.Combine(_environment.WebRootPath, "EmailTemplates") + "/resetPassword.html";
            var htmlTemplate = File.ReadAllText(path);
            var resetPasswordTemplate = htmlTemplate.Replace("{{Name}}", admin.FirstName).Replace("{{BaseUrl}}", _appEndpointSettings.FrontendBaseUrl)
                .Replace("{{ResetLink}}", passwordResetLink);
            await _emailService.EmailRequest(new EmailRequest
            {
                Subject = "Set Password",
                Message = resetPasswordTemplate,
                Email = request.Email
            });

            return BaseResponse.Success();
        }
        string error = result.Errors.FirstOrDefault().Description;
        _logger.LogInformation($"Create Admin terminated [Reason : Creating user failed | Error : {error}]\n");
        return BaseResponse.Failure("06", error);
    }

    public async Task<BaseResponse> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync($"{request.Email}.admin");
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
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return BaseResponse.Success();
        }
        else if (!userPassword.Succeeded && userPassword.Errors.Any(x => x.Code == "InvalidToken"))
        {
            _logger.LogInformation($"Reset Password terminated [ Reason :Invalid Token | Email : {request.Email}] \n");
            return BaseResponse.Failure("12", "Invalid Token");
        }
        return BaseResponse.Failure("12", userPassword.Errors.FirstOrDefault().Description);
    }

    private async Task<BaseResponse> ValidatePassword(ApplicationUser admin , string password )
    {
        var passwordCheck = await _signInManager.PasswordSignInAsync(admin, password, false, true);
        if (passwordCheck.IsLockedOut)
        {
            _logger.LogInformation($"Login terminated [Reason : User is locked out | Email : {admin.UserName}] \n");
            return BaseResponse.Failure("12", " Your account is locked please try again with correct email and/or password in 60 minutes");
        }
        if (!passwordCheck.Succeeded)
        {
            _logger.LogInformation($"Login terminated [Reason : Password Check Failed | Email : {admin.UserName}] \n");
            return BaseResponse.Failure("12", "Email or password do not match for an existing user");
        }
        return BaseResponse.Success();
    }
}
