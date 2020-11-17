using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Helpers.ThirdPartyAPI
{
    public class AppEndpoint
    {
        public APIUri APIUri { get; set; }        
    }
    public class APIUri
    {
        public string HealthBancSignIn { get; set; }
        public string HealthBancResendEmail { get; set; }
        public string HealthBancForgotPassword { get; set; }
        public string HealthBancApiBase { get; set; }
        public string FiorianoBaseAddress { get; set; }
        public string FiorianoADAuthentication { get; set; }
        public string PayStackTokenisationBaseAddress { get; set; }
        public string PayStackTokenisationChargeCard { get; set; }
        public string PayStackTokenisationSendOtp { get; set; }
        public string PayStackTokenisationSubmitPhone { get; set; }
        public string PayStackTokenisationaSubmitBirthDay { get; set; }
        public string PayStackTokenisationValidateCharge { get; set; }
        public string PaystackPaymentBaseAddress { get; set; }
        public string PaystackPaymentInsertSubscription { get; set; }
        public string PaystackPaymentUpdateSubscription { get; set; }
        public string PaystackPaymentCancelSubscription { get; set; }
        public string PaystackPaymentGetSubscription { get; set; }
    }
}
