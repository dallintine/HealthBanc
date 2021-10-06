using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Helpers
{
    public class SubscriptionDuration
    {
        public bool FreeTrial { get; set; }
        public double FreeTrialDayDuration { get; set; }
        public double FailedDebitRetrialDuration { get; set; }
        public double FailedIndividualPaymentRetryA { get; set; }
        public double FailedIndividualPaymentRetryB { get; set; }
    }
}