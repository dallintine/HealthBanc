using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Messages.Events;
using HealthBanc.RabbitMq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Handlers.HealthMallAdmin
{
    public class AdminDeletedCreatedHandler : IEventHandler<AdminDeletedCreated>
    {
        private readonly IApplicationUserRepository _userRepository;

        public AdminDeletedCreatedHandler(IApplicationUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task HandleAsync(AdminDeletedCreated @event, ICorrelationContext context)
        {
            var user = await _userRepository.FindByIdAsync(@event.AdminId);
            if(user != null)
            {
                user.AdminId = null; user.SuperAdminId = user.Id;
                _userRepository.Update(user);
                await _userRepository.Save();
                await Task.CompletedTask;
            }
            await Task.CompletedTask;
        }
    }
}
