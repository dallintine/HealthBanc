using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTO
{
    public class HelloEmail
    {
        [JsonProperty("token")]
        public string token { get; set; }
        public string company { get; set; }
        public string HealthServiceProviderName { get; set; }
        public string HealthServiveProviderType { get; set; }
        public string EmailAddress { get; set; }
        public string PhoneNumber { get; set; }
        public string UserName { get; set; }
        public string PremiumAmount { get; set; }
        public string EnroleeNumber { get; set; }
    }
}
