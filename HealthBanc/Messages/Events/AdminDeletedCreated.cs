using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Messages.Events
{
    [MessageNamespace("upanddownline")]
    public class AdminDeletedCreated : IEvent
    {
        public AdminDeletedCreated()
        {
        }

        public AdminDeletedCreated(int superAdminId, int adminId)
        {
            SuperAdminId = superAdminId;
            AdminId = adminId;
        }

        public int SuperAdminId { get; set; }
        public int AdminId { get; set; }
    }
}
