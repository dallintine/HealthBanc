using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Request.Tokenize
{
    public class ValidateCharge
    {
        public ValidateCharge()
        {

        }
        public ValidateCharge(string reference)
        {
            this.reference = reference;
        }

        public string reference { get; set; }
    }
}
