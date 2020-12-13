using System;
using System.Collections.Generic;
using System.Text;

namespace Application.API_RequestModel.Paystack
{
    class SendOtp
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
