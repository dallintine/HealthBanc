using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Vendor : BaseEntity
    {
        public string Name { get; set; }
        public string SettlementAccount { get; set; }
        public long ServiceId { get; set; }
        public Service Service { get; set; }
        public List<Plan> Plans { get; set; }
    }
}
