using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Payment.DTO
{
    public class InitializePaymentRequest
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
        public Metadata Metadata { get; set; }
    }

    public class Metadata
    {
        [JsonProperty("custom_fields")]
        public List<CustomField> Custom_fields { get; set; }
    }

    public class CustomField
    {
        [JsonProperty("SubscriptionId")]
        public long SubscriptionId { get; set; }
    }
}
