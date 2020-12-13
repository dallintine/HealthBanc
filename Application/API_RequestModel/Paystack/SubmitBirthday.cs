using System;
using System.Collections.Generic;
using System.Text;

namespace Application.API_RequestModel.Paystack
{
    class SubmitBirthday
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
