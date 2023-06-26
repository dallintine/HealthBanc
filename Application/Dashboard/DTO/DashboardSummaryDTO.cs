using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dashboard.DTO
{
    public class DashboardSummaryQuery
    {
        public long CustomersCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalIncome { get; set; }
        public long TransactionCount { get; set; }
        public long VendorsCount { get; set; }
    }
}
