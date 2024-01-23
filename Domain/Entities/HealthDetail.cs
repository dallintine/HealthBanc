using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class HealthDetail : BaseEntity
    {
        public string Height { get; set; }

        public string Weight { get; set; }

        public string Allergies { get; set; }
    }
}
