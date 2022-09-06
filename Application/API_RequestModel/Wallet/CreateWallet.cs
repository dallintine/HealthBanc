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
        public string CURRENCYCODE { get; set; } = "NGN";

        [JsonProperty("AccountTier")]
        public string AccountTier { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("ChannelID")]
        public int ChannelId { get; set; }

        [JsonProperty("ProductID")]
        public int ProductId { get; set; }

        public bool MobileNotNuban { get; set; };
    }
}
