using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using AutoMapper;
using HealthBanc.Data;
using HealthBanc.DataAccess.Implementation;
using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Dispatchers;
using HealthBanc.Domain.Models;
using HealthBanc.Handlers.HealthMallAdmin;
using HealthBanc.Helpers.Jwt_Authorization;
using HealthBanc.Infrastructure.Mail;
using HealthBanc.Messages.Events;
using HealthBanc.RabbitMq;
using HealthBanc.Services;
using HealthBanc.Services.EncryptionService;
using HealthBanc.Services.Identity;
using HealthBanc.Services.ImageService;
using HealthBanc.Services.Insurance;
using HealthBanc.Services.Tokenization;
using MediatR;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Formatter;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OData.Edm;
using Microsoft.OpenApi.Models;
using OfficeOpenXml;
using Polly;

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
            services.AddHsts(options =>
            {
                //options.IncludeSubDomains = true;
                //options.MaxAge = TimeSpan.FromDays(365);
            });
            //services.AddOData();

            services.AddControllersWithViews();

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

            ///////////////Add Swagger Service/////////////////////////
            //

            //SetOutputFormatters(services);


            /////////////////////////////////////Register Services//////////////////////////////

            services.AddAutoMapper(typeof(Startup));

            services.AddScoped<IdentityService>();
            services.AddScoped<IEmailSender, EmailSender>();
            services.AddScoped<IImageService, ImageService>();
            services.AddScoped<IApplicationUserRepository,ApplicationUserRepository>();
            services.Configure<AuthMessageSenderOption>(Configuration);
            services.AddScoped<IEncryptAndDecrypt, EncryptAndDecrypt>();
            services.AddScoped<IClassOrRoleRepository, ClassOrRoleRepository>();
            services.AddScoped<IServiceRepository, ServiceRepository>();
            services.AddScoped<InsuranceService>();
            services.AddScoped<IBackendAdminRepository, BackendAdminRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IAxaMansardUserProfileRepository, AxaMansardUserProfileRepository>();
            services.AddScoped<ExcelPackage>();
            services.AddScoped<TokenizationService>();
            services.AddScoped<IAxaMansardCompletionRepository,AxaMansardCompletionRepository>();
            services.AddScoped<SendLogViaWhatApp>();
            services.AddScoped<ITokenizationReferenceRepository, TokenizationReferenceRepository>();

            services.AddIdentity<ApplicationUser, AppRole>(options =>
            {
                options.SignIn.RequireConfirmedEmail = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 4;
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
                //options.TokenLifespan = TimeSpan.FromDays(2));
                 options.TokenLifespan = TimeSpan.FromMinutes(2));



            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")
                , options => options.EnableRetryOnFailure(
                  maxRetryCount: 10, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null)));

            /////////////////////////////////////Register Services//////////////////////////////



            ///////Add http client

            services.AddHttpClient("Fiorano", client =>
            {
                client.BaseAddress = new Uri("http://172.18.4.77:1880/restgateway/services/");
            })
                .AddTransientHttpErrorPolicy(x =>
                x.WaitAndRetryAsync(1, _ => TimeSpan.FromMilliseconds(300)));

            services.AddHttpClient("Paystack", client =>
            {
                client.BaseAddress = new Uri("http://flutterapi.sterlingapps.p.azurewebsites.net/");
            })
              .AddTransientHttpErrorPolicy(x =>
              x.WaitAndRetryAsync(1, _ => TimeSpan.FromMilliseconds(300)));

            services.AddHttpClient("PaystackPayment", client =>
            {
                client.BaseAddress = new Uri("https://dfs.sterlingapps.p.azurewebsites.net/");
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
                            .AllowAnyHeader()
                            .SetPreflightMaxAge(TimeSpan.FromSeconds(3600)));
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
            }).AddJwtBearer(/*JwtBearerDefaults.AuthenticationScheme,*/ options =>
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
            builder.AddRabbitMq();
            builder.AddDispatchers();
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
            IBackendAdminRepository backendAdminRepository)
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
            app.UseCors("CorsPolicy");

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

            var swaggerOptions = new Helpers.SwaggerOptions();
            Configuration.GetSection(nameof(Helpers.SwaggerOptions)).Bind(swaggerOptions);

            app.UseSwagger(option => { option.RouteTemplate = swaggerOptions.JsonRoute; });

            app.UseSwaggerUI(option =>
            {
                option.SwaggerEndpoint(swaggerOptions.UiEndpoint, swaggerOptions.Description);
            });

            app.UseAuthentication();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                //endpoints.EnableDependencyInjection(); 
                //endpoints.Select().OrderBy().Filter().SkipToken().MaxTop(4).Expand().Count();
                //endpoints.MapODataRoute("odata", "odata", GetEdmModel());
            });

            app.UseRabbitMq()
                .SubscribeEvent<ServiceUsedCreated>()
                .SubscribeEvent<AdminDeletedCreated>();

            applicationLifetime.ApplicationStopped.Register(() =>
            {
                //consulClient.Agent.ServiceDeregister(serviceId);
                AutofacContainer.Dispose();
            });
        }

        //IEdmModel GetEdmModel()
        //{
        //    var builder = new ODataConventionModelBuilder();
        //    builder.EntitySet<WeatherForecast>("WeatherForecast");
        //    return builder.GetEdmModel();
        //}

        //private static void SetOutputFormatters(IServiceCollection services)
        //{
        //    services.AddMvcCore(options =>
        //    {
        //        IEnumerable<ODataOutputFormatter> outputFormatters =
        //            options.OutputFormatters.OfType<ODataOutputFormatter>()
        //                .Where(foramtter => foramtter.SupportedMediaTypes.Count == 0);

        //        foreach (var outputFormatter in outputFormatters)
        //        {
        //            outputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/odata"));
        //        }
        //    });
        //}
    }
}
