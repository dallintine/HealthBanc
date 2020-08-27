using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Domain.Models
{
    public class AxaMansardCompany
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<AxaMansardUserProfile> AxaMansardUserProfiles { get; set; }
    }
}
