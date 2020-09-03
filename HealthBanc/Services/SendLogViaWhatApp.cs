using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twilio.Rest.Api.V2010.Account;

namespace HealthBanc.Services
{
    public class SendLogViaWhatApp
    {
        public void SendLog(string body)
        {
            var message = MessageResource.Create(
                from: new Twilio.Types.PhoneNumber("whatsapp:+14155238886"),
                body: body,
                to: new Twilio.Types.PhoneNumber("whatsapp:+2347034770338")
            );
        }
    }
}
