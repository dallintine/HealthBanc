using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Product : BaseEntity
    {
        public string Name { get; set; }
        public string SettlementAccount { get; set; }
        public List<Plan> Plans { get; set; }
        public List<Subscription> Subscriptions { get; set; }
    }
}
