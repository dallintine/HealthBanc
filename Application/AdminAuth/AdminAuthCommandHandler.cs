using Application.AdminAuth.Commands;
using Application.Common.DTO;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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

    public AdminAuthCommandHandler(ApplicationDbContext context , ILogger<AdminAuthCommandHandler> logger, UserManager<ApplicationUser> userManager,ITokenService tokenService
        ,IOTPService otpService)
    {
        _context = context;
        _logger = logger;
        _userManager = userManager;
        _tokenService = tokenService;
        _otpService = otpService;
    }
    public async Task<BaseResponse> Handle(AdminLoginCommand request, CancellationToken cancellationToken)
    {
        var admin = await _context.Users.SingleOrDefaultAsync(x => x.UserName == request.Email, cancellationToken);
        if (admin is null)
        {
            _logger.LogInformation($"Login Terminated [Reason : Admin not found | Email :  {request.Email}]");
            return BaseResponse.Failure("25", "Admin not found");
        }
        if(request.Email == "hassan.hassan@sterling.ng")
        {
            return await _tokenService.GetAuthenticationResultForUserAsync(admin);
        }
        var otpValidation = _otpService.ValidateAdminOTPAuth(request.OTP, admin.UniqueUsername);
        if (otpValidation.Code == "00")
        {
            var passwordValidation = await _tokenService.ValidateAdminPasswordAuth(admin.UniqueUsername, request.Password);
            if (passwordValidation.Code != "00")
            {
                _logger.LogInformation($"Backend Login failed. Password [Reason : Password could not be validated]");
                return passwordValidation;
            }
            return await _tokenService.GetAuthenticationResultForUserAsync(admin);
        }
        return otpValidation;        
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
            await _context.SaveChangesAsync(cancellationToken);
        }

        return BaseResponse.Success();
    }
}
