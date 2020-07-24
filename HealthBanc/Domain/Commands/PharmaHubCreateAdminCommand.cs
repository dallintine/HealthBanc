using HealthBanc.Domain.Events;
using HealthBanc.Messaging.Core.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Domain.Commands
{
    public class PharmaHubCreateAdminCommand : PharmaHubAdminCommand
    {
        public PharmaHubCreateAdminCommand(PharmaHubCreateAdminEvent @event)
        {
            Id = @event.Id;
            StockOrderLimit = @event.StockOrderLimit;
            FirstName = @event.FirstName;
            LastName = @event.LastName;
            Email = @event.Email;
            PhoneNumber = @event.PhoneNumber;
            SuperAdminId = @event.SuperAdminId;
            SuperAdminEmail = @event.SuperAdminEmail;
            ClassOrRoleId = @event.ClassOrRoleId;
        }
    }
}
