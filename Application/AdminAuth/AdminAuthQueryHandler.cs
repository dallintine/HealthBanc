using Application.AdminAuth.Queries;
using Application.Common.ConfigSettings;
using Application.Common.DTO;
using Application.Common.Interfaces;
using Application.Payment.Commands;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Application.AdminAuth
{
    public class AdminAuthQueryHandler : IRequestHandler<SendAdminResetEmailQuery, BaseResponse>
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly AppEndpointSettings _appEndpointSettings;

        public AdminAuthQueryHandler(ApplicationDbContext context, IEmailService emailService, UserManager<ApplicationUser> userManager,
            IOptions<AppEndpointSettings> appEndpointSettings, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
            _emailService = emailService;
            _userManager = userManager;
            _appEndpointSettings = appEndpointSettings.Value;
        }

        public async Task<BaseResponse> Handle(SendAdminResetEmailQuery request, CancellationToken cancellationToken)
        {
            var admins = await _context.Users.Where(x => x.Email.Contains(".admin") && !x.IsDeleted && !x.EmailConfirmed).ToListAsync(cancellationToken: cancellationToken);

            foreach (var admin  in admins)
            {
                var userEmail = admin.Email.Replace(".admin", "");
                var token = await _userManager.GeneratePasswordResetTokenAsync(admin);
                string passwordResetLink = $"{_appEndpointSettings.FrontendAdminBaseUrl}{_appEndpointSettings.AdminChangePassword}?email={HttpUtility.UrlEncode(userEmail)}" +
                    $"&emailToken={HttpUtility.UrlEncode(token)}";
                var path = Path.Combine(_environment.WebRootPath, "EmailTemplates") + "/resetPassword.html";
                var htmlTemplate = File.ReadAllText(path);
                var resetPasswordTemplate = htmlTemplate.Replace("{{Name}}", admin.FirstName).Replace("{{BaseUrl}}", _appEndpointSettings.FrontendBaseUrl)
                    .Replace("{{ResetLink}}", passwordResetLink);
                await _emailService.EmailRequest(new EmailRequest
                {
                    Subject = "Set Password",
                    Message = resetPasswordTemplate,
                    Email = userEmail
                });
            }

            return BaseResponse.Success();
        }
    }
}
