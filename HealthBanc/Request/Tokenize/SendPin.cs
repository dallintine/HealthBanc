using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Request.Tokenize
{
    public class SendPin
    {
        public SendPin()
        {

        }
        public SendPin(string pin, string reference)
        {
            this.pin = pin;
            this.reference = reference;
        }

        public string pin { get; set; }
        public string reference { get; set; }
    }
}
