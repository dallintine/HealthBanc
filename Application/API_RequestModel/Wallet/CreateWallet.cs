using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.API_RequestModel.Wallet
{
    public class CreateWallet
    {
        [JsonProperty("firstname")]
        public string Firstname { get; set; }

        [JsonProperty("lastname")]
        public string Lastname { get; set; }

        [JsonProperty("mobile")]
        public string Mobile { get; set; }

        [JsonProperty("DOB")]
        public DateTime DOB { get; set; }

        [JsonProperty("Gender")]
        public string Gender { get; set; }

        [JsonProperty("CURRENCYCODE")]
        public string CURRENCYCODE { get; set; }

        [JsonProperty("AccountTier")]
        public string AccountTier { get; set; }
    }
}
