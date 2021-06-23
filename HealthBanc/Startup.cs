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
using Hangfire;
using Infrastructure.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
using Hangfire.Dashboard;
using Application.Services.HealthInsured_AxaMansard.Insurance;

namespace HealthBanc
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHangfire(x => x.UseSqlServerStorage(Configuration.GetConnectionString("HangfireConnection")));
            services.AddHangfireServer();

            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

            /////////////////////////////////////Register Services//////////////////////////////

            services.AddAutoMapper(typeof(Startup));
            services.AddDataAccessServices();

            services.Configure<SubscriptionDuration>(Configuration.GetSection("SubscriptionDuration"));
            services.Configure<Paystack>(Configuration.GetSection("Paystack"));
            services.Configure<AxaMansardConfiguration>(Configuration.GetSection("AxaMansardConfiguration"));
            services.Configure<Application.Helpers.Environment>(Configuration.GetSection("Environment"));
            services.Configure<SterlingOtpConfig>(Configuration.GetSection("SterlingOtpConfig"));
            services.Configure<AppEndpoint>(Configuration);
            services.Configure<ImageStorage>(Configuration.GetSection("ImageStorage"));
            services.Configure<EmailAuth>(Configuration.GetSection("EmailAuth"));
            services.Configure<HygeiaConfiguration>(Configuration.GetSection("HygeiaConfiguration"));
            services.Configure<AdminAuthSettings>(Configuration.GetSection("AdminAuthSettings"));
            services.Configure<HMOAccountDetails>(Configuration.GetSection("HMOAccountDetails"));
            services.Configure<IBSConfig>(Configuration.GetSection("IBSConfig"));
            
            services.AddIdentity<ApplicationUser, AppRole>(options =>
            {
                options.SignIn.RequireConfirmedEmail = true;                
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
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

            var email = Configuration.GetSection("EmailAuth");
            services.Configure<EmailAuth>(email);
            var emailValues = email.Get<EmailAuth>();

            services.AddHttpClient("EmailSender", client =>
            {
                client.BaseAddress = new Uri(emailValues.EmailNotificationBaseUrl);
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

            var hygeia = Configuration.GetSection("HygeiaConfiguration");
            services.Configure<HygeiaConfiguration>(hygeia);
            var hygeiaValues = hygeia.Get<HygeiaConfiguration>();

            services.AddHttpClient("Hygeia", client =>
            {
                client.BaseAddress = new Uri(hygeiaValues.HygeiaBaseAddress);
            })
             .AddTransientHttpErrorPolicy(x =>
             x.WaitAndRetryAsync(1, _ => TimeSpan.FromMilliseconds(300)));

            var sterlingOTPConfig = Configuration.GetSection("SterlingOtpConfig");
            services.Configure<SterlingOtpConfig>(sterlingOTPConfig);
            var sterlingOTPConfigValues = sterlingOTPConfig.Get<SterlingOtpConfig>();



            //---------------------------- CORS setting---------------------------------------------------------//
            services.AddCors(options =>
            {
                options.AddPolicy("Cors",
                    builder =>
                    builder.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod());
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
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, Serilog.ILogger logger, TokenValidationParameters tokenValidationParameters,TokenizationService tokenizationService)
        {
            tokenizationService.MakeHygeiaHMOPayment().Wait();
            tokenizationService.MakeAxamansardHMOPayment().Wait();

            var options = new DashboardOptions
            {
                Authorization = new IDashboardAuthorizationFilter[]
                {
                    new MyAuthorizationFilter(tokenValidationParameters, logger,"Super-Administrator")
                }
            };
            //app.UseHangfireDashboard($"/apiResponse1963.", new DashboardOptions
            //{
            //    Authorization = new[] { new MyAuthorizationFilter() }
            //});
            app.UseHangfireDashboard("/apiResponse1963.", options);

            ServicePointManager.ServerCertificateValidationCallback +=
               (sender, certificate, chain, errors) =>
               {
                   return true;
               };

            app.ConfigureExceptionHandler(logger);

            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Add("X-Frame-Options", "DENY");
                context.Response.Headers.Add("Referrer-Policy", "no-referrer-when-downgrade");
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
            app.UseCors("Cors");
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
