using HealthBanc.Messaging.Core.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Messaging.Core.Commands
{
    public abstract class Command : Message
    {
        public DateTime Timestamp { get;  protected set; }
        protected Command()
        {
            Timestamp = DateTime.Now;
        }
    }
}
