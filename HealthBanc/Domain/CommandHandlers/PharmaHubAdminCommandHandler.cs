using HealthBanc.Domain.Commands;
using HealthBanc.Domain.Events;
using HealthBanc.Messaging.Core.Bus;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HealthBanc.Domain.CommandHandlers
{
    public class PharmaHubAdminCommandHandler : IRequestHandler<PharmaHubCreateAdminCommand, bool>
    {
        private readonly IEventBus _bus;

        public PharmaHubAdminCommandHandler(IEventBus bus)
        {
            _bus = bus;
        }
        public Task<bool> Handle(PharmaHubCreateAdminCommand request, CancellationToken cancellationToken)
        {
            _bus.Publish(new PharmaHubCreateAdminEvent(request));
            return Task.FromResult(true);
        }
    }
}
