using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.API_ResponseModel.Wallet
{
    public class WalletToAccountResponse
    {
        [JsonProperty("sent")]
        public bool Sent { get; set; }
        [JsonProperty("paymentreference")]
        public string Paymentreference { get; set; }
    }
}
