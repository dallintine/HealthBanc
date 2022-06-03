using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Helpers
{
    public class WalletSettings
    {
        public string BaseUrl { get; set; }
        public string CreateWallet { get; set; }
        public string WalletToSterling { get; set; }
        public string WalletDetails { get; set; }
        public string SecretKey { get; set; }
        public string IV { get; set; }
        public string TransferType { get; set; }
        public string CURRENCYCODE { get; set; }
        public string Toacct { get; set; }
        public string ProductId { get; set; }
        public string ChannelId { get; set; }    
    }
}