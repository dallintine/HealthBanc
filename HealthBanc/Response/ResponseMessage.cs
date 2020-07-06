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
        public object Data { get; set; }
    }
}
