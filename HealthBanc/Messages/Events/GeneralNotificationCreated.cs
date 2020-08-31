using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Messages.Events
{
    public class GeneralNotificationCreated : IEvent
    {
        public string SenderDescription { get; set; }
        public string Description { get; set; }
        public string Details { get; set; }
        public DateTime Date { get; set; }

        public GeneralNotificationCreated()
        {

        }
        public GeneralNotificationCreated(string senderDescription, string description, string details, DateTime date)
        {
            SenderDescription = senderDescription;
            Description = description;
            Details = details;
            Date = date;
        }
    }
}
