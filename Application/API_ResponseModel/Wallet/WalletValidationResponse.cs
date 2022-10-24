using System;
using System.Collections.Generic;
using System.Text;

namespace Application.API_ResponseModel.Wallet
{
    public class WalletValidationResponse
    {
        public int Id { get; set; }
        public string Customerid { get; set; }
        public string Firstname { get; set; }
        public string Nuban { get; set; }
        public float Availablebalance { get; set; }
        public string Lastname { get; set; }
        public string Fullname { get; set; }
        public string Mobile { get; set; }
        public string Phone { get; set; }
        public string Gender { get; set; }
        public string Email { get; set; }
        public string Currencycode { get; set; }
        public int Restind { get; set; }
        public string VIRTUALACCT { get; set; }
        public string ACCOUNTTIER { get; set; }
    }
}
