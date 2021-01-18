using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Helpers
{
    public class SendGridTemplateId
    {
        public string VerifyEmail { get; set; }
        public string ForgotPassowrd { get; set; }
        public string HealthInsured_PaymentReminder { get; set; }
        public string HeliumHealth { get; set; }
        public string SuccessfulSubscription { get; set; }
        public string FailedDebit { get; set; }
    }
}
