using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Subscriptions.DTO
{
    public class PaymentSummaryDTO
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalIncome { get; set; }
        public long TransactionCount { get; set; }
    }
}
