using System;
using System.Collections.Generic;
using System.Text;

namespace Application.API_RequestModel.Wallet
{
    public class WalletToAccount
    {
        public string Amt { get; set; }
        public string Toacct { get; set; }
        public string Frmacct { get; set; }
        public string PaymentRef { get; set; }
        public string Remarks { get; set; }
        public int ChannelID { get; set; }
        public string CURRENCYCODE { get; set; }
        public int TransferType { get; set; }
    }
}
