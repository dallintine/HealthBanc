using System;
using System.Collections.Generic;
using System.Text;

namespace Application.API_RequestModel.Wallet
{
    public class EncryptedModel
    {
        public EncryptedModel()
        {

        }
        public EncryptedModel(string data)
        {
            Data = data;
        }

        public string Data { get; set; }
    }
}
