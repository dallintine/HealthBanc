using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Plan : BaseEntity
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public double MarkUpRate { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }
    }
}
