using HealthBanc.Messaging.Core.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Domain.Commands
{
    public class PharmaHubAdminCommand : Command
    {
        public int Id { get; set; }
        public int StockOrderLimit { get; protected set; }
        public string FirstName { get; protected set; }
        public string LastName { get; protected set; }
        public string Email { get; protected set; }
        public string PhoneNumber { get; protected set; }
        public int SuperAdminId { get; protected set; }
        public string SuperAdminEmail { get; protected set; }
        public int ClassOrRoleId { get; protected set; }
    }
}
