using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Response
{
    public class ResponseMessage
    {
        public bool Status { get; set; }
        public int ResponseCode { get; set; }
        public string Message { get; set; }
        public string AuthorizationCode { get; set; }
        public object Data { get; set; }
    }

    public class ResponseMessage<T>
    {
        public bool Status { get; set; }
        public int ResponseCode { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
    }

    public class ResponseInsure
    {
        public bool Status { get; set; }
        public int ResponseCode { get; set; }
        public string Message { get; set; }
        public string Data { get; set; }
    }

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
