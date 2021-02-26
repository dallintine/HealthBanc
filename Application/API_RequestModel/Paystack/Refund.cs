using System;
using System.Collections.Generic;
using System.Text;

namespace Application.API_RequestModel.Paystack
{
    public class Refund
    {
        public Refund()
        {
                
        }
        public Refund(string transactions, string amounts)
        {
            transaction = transactions;
            amount = amounts;
        }

        public string transaction { get; set; }
        public string amount { get; set; }
    }
}
