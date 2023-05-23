using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Plans
{
    public class PlanDTO
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public string VendorName { get; set; }
        public string ImageURL { get; set; }
        public string Tag { get; set; }
        public string ExternalLinkName { get; set; }
        public string ExternalLinkURL { get; set; }
        public bool OptionalFee { get; set; }
        public List<PlanDescriptionDTO> PlanDescriptionDTOs { get; set; }
    }
}
