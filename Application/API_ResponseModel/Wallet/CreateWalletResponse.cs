using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.API_ResponseModel.Wallet
{
    public class CreateWalletResponse
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("response")]
        public string Response { get; set; }

        [JsonProperty("responsedata")]
        public string Responsedata { get; set; }

        [JsonProperty("data")]
        public Data Data { get; set; }
    }

    public class Data
    {
        [JsonProperty("firstname")]
        public string Firstname { get; set; }

        [JsonProperty("lastname")]
        public string Lastname { get; set; }

        [JsonProperty("email")]
        public object Email { get; set; }

        [JsonProperty("mobile")]
        public string Mobile { get; set; }

        [JsonProperty("AcctTier")]
        public string AcctTier { get; set; }

        [JsonProperty("VIRTUALACCT")]
        public string VIRTUALACCT { get; set; }

        [JsonProperty("Nuban")]
        public object Nuban { get; set; }
    }
}
