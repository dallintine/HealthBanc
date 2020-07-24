using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Messaging.Core.Events
{
    public abstract class Event
    {
        public DateTime Timestamp { get; set; }
        public Event()
        {
            Timestamp = DateTime.Now;
        }
    }
}
