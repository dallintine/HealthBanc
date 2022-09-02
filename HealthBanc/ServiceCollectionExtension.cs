using Application.AuditAndReport.AuditLog;
using Application.HealthInsured_AxaMansard_Service.Insurance;
using Application.Interfaces;
using Application.Services;
using Application.Services.Activation_Deactivation;
using Application.Services.Admin;
using Application.Services.AuditAndReport;
using Application.Services.Card;
using Application.Services.HealthInsured;
using Application.Services.HealthInsured.Insurance;
using Application.Services.HealthInsured_AxaMansard.Insurance;
using Application.Services.Identity;
using Application.Services.Paystack;
using Application.Services.Wallet;
using DataAccess;
using DataAccess.Logs.Implementation;
using DataAccess.Logs.Interfaces;
using Infrastructure.EncryptionService;
using Infrastructure.ImageService;
using Infrastructure.Mail;
using Infrastructure.PasswordManager;
using Infrastructure.ProcessUniqueIdentifier;
using Infrastructure.SMS;
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
using System.Reflection;

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

            services.AddScoped<IRepositoryWrapper, RepositoryWrapper>();
            services.AddScoped<Activity_ErrorLogService>();
            services.AddScoped<IdentityService>();
            services.AddScoped<HMOIntegrationService>();
            services.AddScoped<BackendAdminService>();
            services.AddScoped<InsurancePSWebHookService>();
            services.AddScoped<CorporateInsuranceService>();
            services.AddScoped<IBSIntegrationService>();
            services.AddScoped<Card_SubscriptionService>();
            services.AddScoped<IEncryptAndDecrypt, EncryptAndDecrypt>();
            services.AddScoped<InsuranceService>();
            services.AddScoped<ExcelPackage>();
            services.AddScoped<OTPService>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IImageService, ImageService>();
            services.AddScoped<IExceptionLogRepository, ExceptionLogRepository>();
            services.AddScoped<IEmailSender, EmailSender>();
            services.AddScoped<IUniqueIdentifier, UniqueIdentifier>();
            services.AddScoped<AuditLogService>();
            services.AddScoped<TokenizationService>();
            services.AddScoped<PaystackService>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddSingleton<IWalletEncryptionsAndDecryption, WalletEncryptionAndDecryption>();
            services.AddSingleton<ISMSService, SMSService>();
            services.AddScoped<Dashboard_Analytics>();
            services.AddScoped<IFileProcessor, FileProcessor>();
            services.AddScoped<LeadGeneratorService>();
            services.AddScoped<FamilyInsuranceService>();
            services.AddScoped<UtilityService>();
            services.AddScoped<ImageService>();
            services.AddScoped<WalletConnect>();
            services.AddScoped<WalletService>();
            services.AddScoped<OTPService>();
            services.AddScoped<RestrictionService>();
            services.AddScoped<WalletPaymentService>();

        }
    }
}
