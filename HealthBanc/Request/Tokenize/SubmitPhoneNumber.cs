using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Request.Tokenize
{
    public class SubmitPhoneNumber
    {
        public SubmitPhoneNumber()
        {

        }
        public SubmitPhoneNumber(string phone, string reference)
        {
            this.phone = phone;
            this.reference = reference;
        }

        public string phone { get; set; }
        public string reference { get; set; }
    }
}
