using HealthBanc.Domain.CommandHandlers;
using HealthBanc.Domain.Commands;
using HealthBanc.Messaging.Core.Bus;
using IdentityApi.Messaging.InfraBus;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityApi.Messaging.InfraIoc
{
    public class DependencyContainer
    {
        public static void RegisterServices(IServiceCollection services)
        {
            //Domain Bus
            services.AddSingleton<IEventBus, RabbitMQBus>(sp =>
            {
                var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
                return new RabbitMQBus(sp.GetService<IMediator>(), scopeFactory);
            });

            //Domain Banking Commands
            services.AddTransient<IRequestHandler<PharmaHubCreateAdminCommand, bool>, PharmaHubAdminCommandHandler>();

            //Subscriptions
            //services.AddTransient<DocumentStatusEventHandler>();

            //Domain 
            //services.AddTransient<IEventHandler<DocumentStatusCreatedEvent>, DocumentStatusEventHandler>();
        }
    }
}
