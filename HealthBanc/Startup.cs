using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using AutoMapper;
using DataAccess.HealthInsured_AxaMansard.Interfaces;
using DataAccess.HealthInsured_AxaMansard.Implementation;
using Hangfire;
using Infrastructure.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Infrastructure.Mail;
using Application.Helpers;
using Application.Helpers.ThirdPartyAPI;
using Infrastructure.EncryptionService;
using HealthBanc.Middleware.GlobalErrorHandling.Extensions;
using Infrastructure.PasswordManager;
using Domain.Models;
using Persistence;
using Application.Helpers.Jwt_Authorization;
using Application.Services.Identity;
using Application.HealthInsured_AxaMansard_Service.AuditAndReport.AuditLog;
using Application.HealthInsured_AxaMansard_Service.Insurance;
using Application.Interfaces;
using Application.Services.HealthInsured_AxaMansard.Insurance;
using DataAccess.General.Interfaces;
using DataAccess.General.Implementation;
using DataAccess.Logs.Implementation;
using DataAccess.Logs.Interfaces;
using OfficeOpenXml;
using Application.Services.Admin;
using Infrastructure.ImageService;
using Application.Services.Paystack;

namespace HealthBanc
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env)
        {
            // In ASP.NET Core 3.0 `env` will be an IWebHostEnvironment, not IHostingEnvironment.
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            this.Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; private set; }

        public ILifetimeScope AutofacContainer { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHangfire(x => x.UseSqlServerStorage(Configuration.GetConnectionString("HangfireConnection")));
            services.AddHangfireServer();

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

            
            /////////////////////////////////////Register Services//////////////////////////////

            services.AddAutoMapper(typeof(Startup));

            services.AddScoped<IdentityService>();
            services.AddScoped<IApplicationUserRepository,ApplicationUserRepository>();
            services.AddScoped<IEncryptAndDecrypt, EncryptAndDecrypt>();
            services.AddScoped<IClassOrRoleRepository, ClassOrRoleRepository>();
            services.AddScoped<IServiceRepository, ServiceRepository>();
            services.AddScoped<InsuranceService>();
            services.AddScoped<ExcelPackage>();
            services.AddScoped<IBackendAdminRepository, BackendAdminRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IAxaMansardUserProfileRepository, AxaMansardUserProfileRepository>();
            services.AddScoped<IAxaMansardCompletionRepository,AxaMansardCompletionRepository>();
            services.AddScoped<ICardRepository, CardRepository>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IImageService, ImageService>();
            services.AddScoped<IPaymentReferenceRepository, PaymentReferenceRepository>();
            services.AddScoped<IExceptionLogRepository, ExceptionLogRepository>();
            services.AddScoped<IUserAuditLogRepository, UserAuditLogRepository>();
            services.AddScoped<IEmailSender, EmailSender>();
            services.AddScoped<IPasswordChangeRepository, PasswordChangeRepository>();
            services.AddScoped<IUserLogin_LogoutLogRepository, UserLogin_LogoutLogRepository>();
            services.AddScoped<IAdminLogin_LogoutLogRepository, AdminLogin_LogoutLogRepository>();
            services.AddScoped<IInsufficientChargeTransactionRepository, IInsufficientChargeTransactionRepository>();
            services.AddScoped<IScheduledAxaEnrollmentRepository, ScheduledAxaEnrollmentRepository>();
            services.AddScoped<IScheduledPaymentRepository, ScheduledPaymentRepository>();
            services.AddScoped<IAxaEnrollmentReactivationRepository, AxaEnrollmentReactivationRepository>();
            services.AddScoped<IPaymentOnReactivationRepository, PaymentOnReactivationRepository>();
            services.AddScoped<IAxaMansardHospitalListRepository, AxaMansardHospitalListRepository>();
            services.AddScoped<AuditLogService>();
            services.AddScoped<TokenizationService>();
            services.AddScoped<PaystackService>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddScoped<IAdminAuditLogRepository, AdminAuditLogRepository>();
            services.Configure<SubscriptionDuration>(Configuration.GetSection("SubscriptionDuration"));
            services.Configure<SendGridTemplateId>(Configuration.GetSection("SendGridTemplateId"));
            services.Configure<Paystack>(Configuration.GetSection("Paystack"));
            services.AddScoped<Dashboard_Analytics>();
            services.Configure<AxaMansardConfiguration>(Configuration.GetSection("AxaMansardConfiguration"));
            services.Configure<Application.Helpers.Environment>(Configuration.GetSection("Environment"));
            services.Configure<AuthMessageSenderOption>(Configuration);
            services.Configure<SterlingOtp>(Configuration);
            services.Configure<AppEndpoint>(Configuration);
            services.Configure<ImageStorage>(Configuration.GetSection("ImageStorage"));

            services.AddIdentity<ApplicationUser, AppRole>(options =>
            {
                options.SignIn.RequireConfirmedEmail = true;                
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 3;
                options.Lockout.DefaultLockoutTimeSpan = DateTime.Now.AddYears(100) - DateTime.Now;
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 1;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
            }).AddEntityFrameworkStores<ApplicationDbContext>().
            AddDefaultTokenProviders();

            services.Configure<DataProtectionTokenProviderOptions>(options =>
                 options.TokenLifespan = TimeSpan.FromDays(5));

            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")
                , options => options.EnableRetryOnFailure(
                  maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null)));

            /////////////////////////////////////Register Services//////////////////////////////



            /////////////////////////////////////////Add http client//////////////////////////////////////////////
            
            var baseUrl = Configuration.GetSection("APIUri");
            services.Configure<APIUri>(baseUrl);
            var baseUrlValues = baseUrl.Get<APIUri>();           

            services.AddHttpClient("Fiorano", client =>
            {
                client.BaseAddress = new Uri(baseUrlValues.FiorianoBaseAddress);
            })
                .AddTransientHttpErrorPolicy(x =>
                x.WaitAndRetryAsync(1, _ => TimeSpan.FromMilliseconds(300)));

            var paystackUrl = Configuration.GetSection("Paystack");
            services.Configure<Paystack>(paystackUrl);
            var paystackUrlValues = paystackUrl.Get<Paystack>();

            services.AddHttpClient("Paystack", client =>
            {
                client.BaseAddress = new Uri(paystackUrlValues.PayStackBaseAddress);
            })
              .AddTransientHttpErrorPolicy(x =>
              x.WaitAndRetryAsync(1, _ => TimeSpan.FromMilliseconds(300)));


            var axaMansard = Configuration.GetSection("AxaMansardConfiguration");
            services.Configure<AxaMansardConfiguration>(axaMansard);
            var axaMansardValues = axaMansard.Get<AxaMansardConfiguration>();

            services.AddHttpClient("AxaMansard", client =>
            {
                client.BaseAddress = new Uri(axaMansardValues.AxaMansardBaseAddress);
            })
             .AddTransientHttpErrorPolicy(x =>
             x.WaitAndRetryAsync(1, _ => TimeSpan.FromMilliseconds(300)));


            //---------------------------- CORS setting---------------------------------------------------------//
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder =>
                        builder.AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader());
            });

            //---------------------------- CORS setting---------------------------------------------------------//

            services.AddAuthorization(options =>
            {
                options.AddPolicy("SuperAdminRole", policy => policy.RequireRole("SuperAdmin").RequireAuthenticatedUser());

                options.AddPolicy("AdminRole", policy => policy.RequireRole("Initiator", "Reviewer", "Authorizer").RequireAuthenticatedUser());

                options.AddPolicy("InitiatorRole", policy => policy.RequireRole("Initiator").RequireAuthenticatedUser());

                options.AddPolicy("ReviewerRole", policy => policy.RequireRole("Reviewer").RequireAuthenticatedUser());

                options.AddPolicy("AuthorizerRole", policy => policy.RequireRole("Authorizer").RequireAuthenticatedUser());

            });

            //------------------------------------JWT Authentication Settings--------------------------------------//
            var appsettings = Configuration.GetSection("JwtSettings");
            services.Configure<JwtSettings>(appsettings);
            var appSettingValues = appsettings.Get<JwtSettings>();

            //Encoding The Secret
            var key = Encoding.ASCII.GetBytes(appSettingValues.Secret);

            /////////////////Authentication Middleware            

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                LifetimeValidator = TokenLifetimeValidator.Validate,
                ValidIssuer = appSettingValues.Site,
                ValidAudience = appSettingValues.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };
            services.AddSingleton(tokenValidationParameters);

            services.AddAuthentication(options =>
            {
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.TokenValidationParameters = tokenValidationParameters;
            });

            //------------------------------------JWT Authentication Settings--------------------------------------//
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            // Register your own things directly with Autofac, like:
            builder.RegisterAssemblyTypes(Assembly.GetEntryAssembly())
                   .AsImplementedInterfaces();           
        }

        public static class TokenLifetimeValidator
        {
            public static bool Validate(
                DateTime? notBefore,
                DateTime? expires,
                SecurityToken tokenToValidate,
                TokenValidationParameters @param
            )
            {
                return (expires != null && expires > DateTime.UtcNow);
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime applicationLifetime, UserManager<ApplicationUser> userManger,
            IBackendAdminRepository backendAdminRepository, Serilog.ILogger logger)
        {
            if (userManger.FindByNameAsync("Hassan.Hassan@sterling.ng").Result == null)
            {
                ApplicationUser user = new ApplicationUser()
                {
                    UniqueUsername = "hassannh",
                    UserName = "hassan.hassan@sterling.ng",
                    Email = "hassan.hassan@sterling.ng",
                    FirstName = "Hassan",
                    LastName = "Hassan",
                    EmailConfirmed = true
                };
                BackendAdminUser adminUser = new BackendAdminUser()
                {
                    Email = "hassan.hassan@sterling.ng",
                    FirstName = "Hassan",
                    LastName = "Hassan",
                    ClassOrRoleId = 6
                };

                var result = userManger.CreateAsync(user).Result;

                if (result.Succeeded)
                {
                    userManger.AddToRoleAsync(user, "Super-Administrator").Wait();
                    backendAdminRepository.Create(adminUser);
                    backendAdminRepository.Save().Wait();
                }
            }

            var hangfireSecret = new JwtSettings();
            Configuration.GetSection(nameof(JwtSettings)).Bind(hangfireSecret);
            app.UseHangfireDashboard($"/apiResponse1963.", new DashboardOptions
            {
                Authorization = new[] { new MyAuthorizationFilter() }
            });

            ServicePointManager.ServerCertificateValidationCallback +=
                (sender, certificate, chain, errors) =>
                {
                    return true;
                };




            app.UseCors("CorsPolicy");
            app.ConfigureExceptionHandler(logger);

            app.UseHsts();

            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Add("X-Frame-Options", "DENY");
                context.Response.Headers.Add("Referrer-Policy", "no-referrer");
                context.Response.Headers.Add("X-Permitted-Cross-Domain-Policies", "none");
                context.Response.Headers.Add("Content-Security-Policy", "unsafe-inline 'self'");
                context.Response.Headers.Add("Feature-Policy", "accelerometer 'none'; camera 'none'; geolocation 'none'; gyroscope 'none'; magnetometer 'none'; microphone 'none';");
                await next();
            });

            app.UseHttpsRedirection();

            var developmentOptions = new Application.Helpers.Environment();
            Configuration.GetSection(nameof(Application.Helpers.Environment)).Bind(developmentOptions);

            if (developmentOptions.Staging)
            {
                var swaggerOptions = new SwaggerOptions();
                Configuration.GetSection(nameof(SwaggerOptions)).Bind(swaggerOptions);

                app.UseSwagger(option => { option.RouteTemplate = swaggerOptions.JsonRoute; });

                app.UseSwaggerUI(option =>
                {
                    option.SwaggerEndpoint(swaggerOptions.UiEndpoint, swaggerOptions.Description);
                });
            }     
            

            

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            applicationLifetime.ApplicationStopped.Register(() =>
            {
                AutofacContainer.Dispose();
            });
        }
    }
}
