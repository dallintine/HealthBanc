using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Payment
{
    public class InitializePaystackPayment
    {
        [JsonProperty("email")]
        public string Email { get; set; }
        [JsonProperty("amount")]
        public string Amount { get; set; }
        [JsonProperty("reference")]
        public string Reference { get; set; }
        [JsonProperty("callback_url")]
        public string Callback_url { get; set; }
        [JsonProperty("metadata")]
        public Metadata Metadata { get; set; } = new Metadata();
    }

    public class Metadata
    {
        [JsonProperty("custom_fields")]
        public List<object> Custom_fields { get; set; }
    }
}
