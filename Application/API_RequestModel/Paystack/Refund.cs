using System;
using System.Collections.Generic;
using System.Text;

namespace Application.API_RequestModel.Paystack
{
    public class Refund
    {
        public Refund(string transaction, string amount)
        {
            Transaction = transaction;
            Amount = amount;
        }

        public string Transaction { get; set; }
        public string Amount { get; set; }
    }
}
