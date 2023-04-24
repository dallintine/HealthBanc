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
        public Product Product { get; set; }
        public long ProductId { get; set; }
        public Plan Plan { get; set; }
        public long PlanId { get; set; }
        public string Status { get; set; }
        public decimal Amount { get; set; }
        public bool IsSuccessfully { get; set; }
    }
}
