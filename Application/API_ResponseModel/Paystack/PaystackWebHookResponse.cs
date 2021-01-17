using System;
using System.Collections.Generic;
using System.Text;

namespace Application.API_ResponseModel.Paystack
{
    public class PaystackWebHookResponse
    {
        public string @event { get; set; }
        public Data data { get; set; }
    }
}
