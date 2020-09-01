using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Request.Tokenize
{
    public class ChargeCard
    {
        public string email { get; set; }
        public string amount { get; set; }
        public Card card { get; set; }
        public string pin { get; set; }
        public string reference { get; set; }
    }
    public class Card
    {
        public string cvv { get; set; }
        public int expiry_month { get; set; }
        public int expiry_year { get; set; }
        public string number { get; set; }
        public string type { get; set; }
    }
}
