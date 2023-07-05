using Application.Common.DTO;
using Application.Common.ConfigSettings;
using Application.Plans;
using DataAccess;
using Domain.Entities;
using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Persistence.Data;
using Polly;
using System.Text;
using Application.Image;

namespace HealthBanc
{
    public static class ServiceCollectionExtension
    {
        public static void AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllersWithViews();
            services.AddMediatR(typeof(Create));
            services.AddFluentValidationAutoValidation().AddFluentValidationClientsideAdapters();
            services.AddValidatorsFromAssemblyContaining<Create>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IRepositoryWrapper, RepositoryWrapper>();
            services.AddAutoMapper(typeof(MappingProfile));


            //---------------------------- CORS setting---------------------------------------------------------//
            services.AddCors(options =>
            {
                options.AddPolicy("CORS",
                    builder =>
                    builder.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod());
            });

            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")
                , options => options.EnableRetryOnFailure(
                  maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null)));

            services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.SignIn.RequireConfirmedEmail = false;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(60);
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 1;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
            }).AddEntityFrameworkStores<ApplicationDbContext>().
            AddDefaultTokenProviders();

            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

            //------------------------------------JWT Authentication Settings--------------------------------------//
            var appsettings = configuration.GetSection("JwtSettings");
            services.Configure<JwtSettings>(appsettings);
            var appSettingValues = appsettings.Get<JwtSettings>();

            //Encoding The Secret
            var key = Encoding.ASCII.GetBytes(appSettingValues.Secret);


            //------------------------------------- Authentication Middleware ---------------------------------------//            

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                ValidateAudience = false,
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
                options.Events = new JwtBearerEvents();
                options.Events.OnChallenge = context =>
                {
                    // Skip the default logic.
                    context.HandleResponse();

                    var payload = BaseResponse.Failure("01", "Unauthorised");
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = 401;

                    return context.Response.WriteAsync(payload.ToString());
                };
            });

            //------------------------Fioriano------------------------------------------------//

            var baseUrl = configuration.GetSection("AppEndpointSettings");
            services.Configure<AppEndpointSettings>(baseUrl);
            var baseUrlValues = baseUrl.Get<AppEndpointSettings>();

            services.AddHttpClient("Fiorano", client =>
            {
                client.BaseAddress = new Uri(baseUrlValues.FiorianoBaseAddress);
            })
                .AddTransientHttpErrorPolicy(x =>
                x.WaitAndRetryAsync(1, _ => TimeSpan.FromMilliseconds(300)));


            // ----------------------------------------Email Config--------------------------------------------//

            var emailSettings = configuration.GetSection("EmailSettings");
            services.Configure<EmailSettings>(emailSettings);
            var emailSettingsValues = emailSettings.Get<EmailSettings>();

            services.AddHttpClient("EmailClient", client =>
            {
                client.BaseAddress = new Uri(emailSettingsValues.EmailNotificationBaseUrl);
            })
             .AddTransientHttpErrorPolicy(x =>
             x.WaitAndRetryAsync(1, _ => TimeSpan.FromMilliseconds(300)));

            var paystackSettings = configuration.GetSection("PaystackSettings");
            services.Configure<PaystackSettings>(paystackSettings);
            var paystackSettingsValues = paystackSettings.Get<PaystackSettings>();

            services.AddHttpClient("PaystackClientClient", client =>
            {
                client.BaseAddress = new Uri(paystackSettingsValues.BaseUrl);
            })
             .AddTransientHttpErrorPolicy(x =>
             x.WaitAndRetryAsync(1, _ => TimeSpan.FromMilliseconds(300)));

            //----------------------------------------  Configuration Settings -------------------------------------------------//

            services.Configure<AppEndpointSettings>(configuration.GetSection("AppEndpointSettings"));
            services.Configure<SMSSettings>(configuration.GetSection("SMSSettings"));
            services.Configure<AzureBlobStorageSettings>(configuration.GetSection("AzureBlobStorageSettings"));
            services.Configure<SterlingOtpSettings>(configuration.GetSection("SterlingOtpSettings"));
            services.Configure<DefaultAdmin>(configuration.GetSection("DefaultAdmin")); 
        }
    }
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

