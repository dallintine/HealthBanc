using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Transaction : BaseEntity
    {
        public string Reference { get; set; }
        public long ApplicationUserId { get; set; }
        public string Email { get; set; }
        public decimal Amount { get; set; }
        public bool IsCompleted { get; set; }
        public List<Subscription> Subscriptions { get; set; }
    }
}
