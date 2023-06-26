using Application.Common.Interfaces;
using Infrastructure.Image;
using Infrastructure.Notifications;
using Infrastructure.OTP;
using Infrastructure.Paystack;
using Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure
{
    public static class InfrastructureServiceExtension
    {
        public static IServiceCollection AddInfrasturctureService(this IServiceCollection services)
        {
            services.AddScoped<IEncryptionService, EncryptionService>();
            services.AddScoped<IHttpConnection, HttpConnection>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<ISMSService, SMSService>();
            services.AddScoped<IOTPService, OTPService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IPaystackService, PaystackService>();
            services.AddScoped<IImageService, ImageService>();

            return services;
        }
    }
}
