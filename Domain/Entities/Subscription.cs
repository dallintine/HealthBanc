using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Subscription : BaseEntity
    {
        public ApplicationUser ApplicationUser { get; set; }
        public long ApplicationUserId { get; set; }
        public Plan Plan { get; set; }
        public long PlanId { get; set; }
        public int Quantity { get; set; }
        public long VendorId { get; set; }

        public Vendor vendor { get; set; }
        public long ServiceId { get; set; }
        public string Status { get; set; }
        public decimal Amount { get; set; }
        public bool OptionalFee { get; set; }
        public long TransactionId { get; set; }
        public string PaymentReference { get; set; }
        public bool IsSuccessful { get; set; }
    }
}
