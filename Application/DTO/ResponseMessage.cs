using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.DTO
{
    public class ResponseMessage
    {
        public bool Status { get; set; }
        public int ResponseCode { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class ResponseMessage<T>
    {
        public bool Status { get; set; }
        public int ResponseCode { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
    }
}
