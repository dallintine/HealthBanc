using System;
using System.Collections.Generic;
using System.Text;

namespace Application.API_RequestModel.Paystack
{
    class SubmitPhoneNumber
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
