using System;
using System.Collections.Generic;
using System.Text;

namespace Application.API_RequestModel.Wallet
{
    public class GetWalletDetails
    {
        public GetWalletDetails()
        {

        }
        public GetWalletDetails(string mobile)
        {
            Mobile = mobile;
        }

        public string Mobile { get; set; }
    }
}
