using Application.Products;
using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;

namespace HealthBanc
{
    public static class ServiceCollectionExtension
    {
        public static void AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMediatR(typeof(List));
            services.AddFluentValidationAutoValidation();
            services.AddValidatorsFromAssemblyContaining<List>();
        }
    }
}
