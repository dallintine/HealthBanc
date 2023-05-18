using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Service : BaseEntity
    {
        public string Name { get; set; }
        public string Tag { get; set; }
        public string ImageUrl { get; set; }
        public List<Vendor> Vendors { get; set; }
    }
}
