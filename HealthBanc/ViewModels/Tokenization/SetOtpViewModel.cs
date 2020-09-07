using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.ViewModels.Tokenization
{
    public class SetOtpViewModel
    {
        public SetOtpViewModel()
        {

        }
        public SetOtpViewModel(string otp, string pin, string reference)
        {
            this.otp = otp;
            this.pin = pin;
            this.reference = reference;
        }

        public string otp { get; set; }
        public string pin { get; set; }
        public string reference { get; set; }
    }
}
