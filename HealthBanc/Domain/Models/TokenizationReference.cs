using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Domain.Models
{
    public class TokenizationReference
    {
        public int Id { get; set; }
        public int SuperAdminId { get; set; }
        public string TokenReference { get; set; }
    }
}
