using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Request.Tokenize
{
    public class SubscribePayment
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
        public string PhoneNumber { get; set; }
        public int RepaymentAmount { get; set; }
        public int Fees { get; set; }
        public DateTime NextRepaymentDate { get; set; }
        public string Channel { get; set; }
        public string RequestId { get; set; }
        public string Token { get; set; }
        public string TokenType { get; set; }
        public string SettlementAccount { get; set; }
    }
}
