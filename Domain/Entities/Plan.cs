using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Plan : BaseEntity
    {
        public string Name { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; }
        public double MarkUpRate { get; set; }
        [ForeignKey("Product")]
        public long VendorId { get; set; }
        public Vendor Vendor { get; set; }
        public long ServiceId { get; set; }
        public Service Service { get; set; }
        public string ImageURL { get; set; }
        public string Tag { get; set; }
        public string ExternalLinkName { get; set; }
        public string ExternalLinkURL { get; set; }
        public bool OptionalFee { get; set; }
        public List<Subscription> Subscriptions { get; set; }
        public List<PlanDescription> PlanDescriptions { get; set; }
    }
}
