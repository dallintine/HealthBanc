using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Request.Tokenize
{
    public class SubmitBirthday
    {
        public SubmitBirthday()
        {

        }
        public SubmitBirthday(DateTime birthday, string reference)
        {
            this.birthday = birthday;
            this.reference = reference;
        }

        public DateTime birthday { get; set; }
        public string reference { get; set; }
    }
}
