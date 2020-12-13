using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Helpers
{
    public class Paystack
    {
        public string SecretKey { get; set; }
        public string PublicKey { get; set; }
        public string PayStackBaseAddress { get; set; }
        public string PayStackChargeCard { get; set; }
        public string PayStackSendOtp { get; set; }
        public string PayStackSubmitPhone { get; set; }
        public string PayStackSubmitBirthDay { get; set; }     
        
        public string ChargeAuthorization { get; set; }
    }
}
