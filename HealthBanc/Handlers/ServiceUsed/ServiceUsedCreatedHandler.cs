using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Messages.Events;
using HealthBanc.RabbitMq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Handlers.ServiceUsed
{
    public class ServiceUsedCreatedHandler : IEventHandler<ServiceUsedCreated>
    {
        private readonly IApplicationUserRepository _userRepository;
        private readonly IServiceRepository _serviceRepository;

        public ServiceUsedCreatedHandler(IApplicationUserRepository userRepository, IServiceRepository serviceRepository)
        {
            _userRepository = userRepository;
            _serviceRepository = serviceRepository;
        }
        public async Task HandleAsync(ServiceUsedCreated @event, ICorrelationContext context)
        {
            var appUser = await _userRepository.GetByEmailAsync(@event.Email);
            if(appUser != null)
            {
                var service = await _serviceRepository.GetServiceById(@event.Id);
                appUser.ServiceUsed = appUser.ServiceUsed +service.Name+",";
                _userRepository.Update(appUser);
                await _userRepository.Save();
                await Task.CompletedTask;
            }
            await Task.CompletedTask;
        }
    }
}
