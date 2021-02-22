using Application.AuditAndReport.AuditLog;
using Application.Interfaces;
using Application.Services.Admin;
using Application.Services.HealthInsured;
using Application.Services.Identity;
using Application.Services.Paystack;
using DataAccess.General.Implementation;
using DataAccess.General.Interfaces;
using DataAccess.HealthInsured.Implementation;
using DataAccess.HealthInsured.Interfaces;
using DataAccess.HealthInsured_AxaMansard.Implementation;
using DataAccess.HealthInsured_AxaMansard.Interfaces;
using DataAccess.Logs.Implementation;
using DataAccess.Logs.Interfaces;
using Infrastructure.EncryptionService;
using Infrastructure.ImageService;
using Infrastructure.Mail;
using Infrastructure.PasswordManager;
using Infrastructure.ProcessUniqueIdentifier;
using Infrastructure.UploadService;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace HealthBanc
{
    public static class ServiceCollectionExtension
    {
        public static void AddDataAccessServices(this IServiceCollection services)
        {
            services.AddHsts(options =>
            {
                //options.IncludeSubDomains = true;
                //options.MaxAge = TimeSpan.FromDays(365);
            });

            services.AddControllersWithViews();

            services.AddApplicationInsightsTelemetry();

            services.AddControllers();

            ///////////////Add Swagger Service/////////////////////////
            services.AddSwaggerGen(x =>
            {
                x.SwaggerDoc("v1", new OpenApiInfo { Title = "HealthBanc", Version = "v1" });

                x.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description =
                     "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                x.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header,
                        },
                        new List<string>()
                    }
                });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                x.IncludeXmlComments(xmlPath);
            });

            services.AddScoped<IdentityService>();
            services.AddScoped<IApplicationUserRepository, ApplicationUserRepository>();
            services.AddScoped<IEncryptAndDecrypt, EncryptAndDecrypt>();
            services.AddScoped<IClassOrRoleRepository, ClassOrRoleRepository>();
            services.AddScoped<IServiceRepository, ServiceRepository>();
            services.AddScoped<InsuranceService>();
            services.AddScoped<ExcelPackage>();
            services.AddScoped<OTPService>();
            services.AddScoped<IBackendAdminRepository, BackendAdminRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IInsuranceProfileRepository, InsuranceProfileRepository>();
            services.AddScoped<IInsuranceCompletionProfileRepository, InsuranceCompletionProfileRepository>();
            services.AddScoped<ICardRepository, CardRepository>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IImageService, ImageService>();
            services.AddScoped<IPaymentReferenceRepository, PaymentReferenceRepository>();
            services.AddScoped<IExceptionLogRepository, ExceptionLogRepository>();
            services.AddScoped<IUserAuditLogRepository, UserAuditLogRepository>();
            services.AddScoped<IEmailSender, EmailSender>();
            services.AddScoped<IUniqueIdentifier, UniqueIdentifier>();
            services.AddScoped<IPasswordChangeRepository, PasswordChangeRepository>();
            services.AddScoped<IUserLogin_LogoutLogRepository, UserLogin_LogoutLogRepository>();
            services.AddScoped<IAdminLogin_LogoutLogRepository, AdminLogin_LogoutLogRepository>();
            services.AddScoped<IScheduledEnrollmentRepository, ScheduledEnrollmentRepository>();
            services.AddScoped<IScheduledPaymentRepository, ScheduledPaymentRepository>();
            services.AddScoped<IEnrollmentReactivationRepository, EnrollmentReactivationRepository>();
            services.AddScoped<IPaymentOnReactivationRepository, PaymentOnReactivationRepository>();
            services.AddScoped<IAxaMansardHospitalListRepository, AxaMansardHospitalListRepository>();
            services.AddScoped<IEnrollmentOnOnboardingRepository, EnrollmentOnOnboardingRepository>();
            services.AddScoped<AuditLogService>();
            services.AddScoped<TokenizationService>();
            services.AddScoped<PaystackService>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddScoped<IAdminAuditLogRepository, AdminAuditLogRepository>();
            services.AddScoped<Dashboard_Analytics>();
            services.AddScoped<ICompanyProfileRepository, CompanyProfileRepository>();
            services.AddScoped<IBeneficiaryReviewRepository, BeneficiaryReviewRepository>();
            services.AddScoped<IHygeiaHospitalListRepository, HygeiaHospitalListRepository>();
            services.AddScoped<IFileProcessor, FileProcessor>();
        }
    }
}
