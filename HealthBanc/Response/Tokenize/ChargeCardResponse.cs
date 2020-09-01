using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Response.Tokenize
{
    public class ChargeCardResponse
    {
        public bool status { get; set; }
        public string message { get; set; }
        public Data data { get; set; }
    }
    public class Data
    {
        public Message message { get; set; }
        public string url { get; set; }
        public string status { get; set; }
        public string reference { get; set; }
    }
    public class Message
    {
    }
}
