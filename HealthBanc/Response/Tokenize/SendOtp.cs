using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Response.Tokenize
{
    public class SendOtp
    {
        public SendOtp()
        {
        }

        public SendOtp(string otp, string reference)
        {
            this.otp = otp;
            this.reference = reference;
        }

        public string otp { get; set; }
        public string reference { get; set; }
    }
}
