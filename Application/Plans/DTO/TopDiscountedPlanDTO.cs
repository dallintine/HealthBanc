using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Plans.DTO
{
    public class TopDiscountedPlanDTO
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public string VendorName { get; set; }
        public string ImageURL { get; set; }
        public string Tag { get; set; }
        public long ServiceId { get; set; }
    }
}
