using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using HealthBanc.Data;
using HealthBanc.DataAccess.Implementation;
using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Domain.Models;
using HealthBanc.Helpers.Jwt_Authorization;
using HealthBanc.Infrastructure.Mail;
using HealthBanc.Messaging.Core.Bus;
using HealthBanc.Messaging.InfraIoc;
using HealthBanc.Services.EncryptionService;
using HealthBanc.Services.Identity;
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

namespace HealthBanc
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddOData();

            services.AddControllers();

            ///////////////Add Swagger Service
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

            //SetOutputFormatters(services);


            /////////////////////////////////////Register Services
            
            services.AddAutoMapper(typeof(Startup));

            services.AddScoped<IdentityService>();
            services.AddScoped<IEmailSender, EmailSender>();
            services.AddScoped<IApplicationUserRepository,ApplicationUserRepository>();
            services.Configure<AuthMessageSenderOption>(Configuration);
            services.AddScoped<IEncryptAndDecrypt, EncryptAndDecrypt>();
            services.AddScoped<IClassOrRoleRepository, ClassOrRoleRepository>();

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
            }).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();

            services.Configure<DataProtectionTokenProviderOptions>(options =>
                options.TokenLifespan = TimeSpan.FromDays(2));


            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")
                , options => options.EnableRetryOnFailure(
                  maxRetryCount: 10, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null)));

            /////////////////////////////////////
            services.AddMediatR(typeof(Startup));
            RegisterServices(services);

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

            //Authentication Middleware
            services.AddAuthentication(options =>
            {
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateIssuerSigningKey = true,
                    ValidateAudience = true,
                    ValidIssuer = appSettingValues.Site,
                    ValidAudience = appSettingValues.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
            });
        }

        private void RegisterServices(IServiceCollection services)
        {
            DependencyContainer.RegisterServices(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCors("CorsPolicy");

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
        }

        private void ConfigureEventBus(IApplicationBuilder app)
        {
            var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();

            //eventBus.Subscribe<DisableUserCreatedEvent, DisableUserEventHandler>();
            //eventBus.Subscribe<UnlockUserCreatedEvent, UnlockUserEventHandler>();
            //eventBus.Subscribe<EnableUserCreatedEvent, EnableUserEventHandler>();
            //eventBus.Subscribe<ChangeIdenitySuperAdminEvent, ChangeIdentitySuperAdminEventHandler>();
            //eventBus.Subscribe<DocumentExistCreatedEvent, DocumentExistEventHandler>();
            //eventBus.Subscribe<DocumentStatusCreatedEvent, DocumentStatusEventHandler>();
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
