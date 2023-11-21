using Application.AdminAuth.Commands;
using Application.Common.ConfigSettings;
using Application.Common.DTO;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.AdminAuth;

public class AdminAuthCommandHandler : IRequestHandler<AdminLoginCommand, BaseResponse>,
IRequestHandler<CreateAdminCommand, BaseResponse>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminAuthCommandHandler> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IOTPService _otpService;
    private readonly IEncryptionService _encryption;
    private readonly DefaultAdmin _defaultAdminSettings;

    public AdminAuthCommandHandler(ApplicationDbContext context , ILogger<AdminAuthCommandHandler> logger, UserManager<ApplicationUser> userManager,ITokenService tokenService
        ,IOTPService otpService, IOptions<DefaultAdmin> defaultAdminSettings,IEncryptionService encryption)
    {
        _context = context;
        _logger = logger;
        _userManager = userManager;
        _tokenService = tokenService;
        _otpService = otpService;
        _encryption = encryption;
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
        //if (decryptedEmail.Item2 == _defaultAdminSettings.Email)
        //{
        //    return await _tokenService.GetAuthenticationResultForUserAsync(admin);
        //}
        if (_defaultAdminSettings.OTPValidation)
        {
            var otpValidation = _otpService.ValidateAdminOTPAuth(request.OTP, admin.UniqueUsername);
            if (otpValidation.Code == "00")
            {
                var passwordValidation = await _tokenService.ValidateAdminPasswordAuth(admin.UniqueUsername, decryptedPassword.Item2);
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
            var passwordValidation = await _tokenService.ValidateAdminPasswordAuth(admin.UniqueUsername, decryptedPassword.Item2);
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
            EmailConfirmed = true,
            UniqueUsername = request.UniqueUsername
        };

        var result = _userManager.CreateAsync(admin).Result;

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(admin, Roles.Admin.ToString());
            _context.Users.Update(admin);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return BaseResponse.Success();
    }
}
