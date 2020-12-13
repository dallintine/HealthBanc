using System;
using System.Collections.Generic;
using System.Text;

namespace Application.API_ResponseModel.Paystack
{
    public class TokenizationResponse
    {
        public bool Status { get; set; }
        public int ResponseCode { get; set; }
        public string Message { get; set; }
        public string AuthorizationCode { get; set; }
        public object Data { get; set; }
        public string LastDigit { get; set; }
        public string Signature { get; set; }
        public string Type { get; set; }
    }
}
