using System;
using System.Collections.Generic;
using System.Text;

namespace Application.API_RequestModel.Paystack
{
    public class ChargeAuthorization
    {
        public string email { get; set; }
        public string amount { get; set; }
        public string authorization_code { get; set; }
        public bool queue { get; set; }
    }
}
