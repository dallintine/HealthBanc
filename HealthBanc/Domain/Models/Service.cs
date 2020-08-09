using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Domain.Models
{
    public class Service
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Notification> Notifications { get; set; }
    }
}
