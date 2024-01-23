using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class ProspectivePartner : BaseEntity
    {
        public string Email { get; set; }
        public string CompanyName { get; set; }
        public string RepresentativeName { get; set; }
        public string PhoneNumber { get; set; }
        public string Location { get; set; }
        public string Category { get; set; }
    }
}
