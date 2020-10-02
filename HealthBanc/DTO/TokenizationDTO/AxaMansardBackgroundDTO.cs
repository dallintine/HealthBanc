using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.DTO.TokenizationDTO
{
    public class AxaMansardBackgroundDTO
    {
        public int Id { get; set; }
        public int SuperAdminId { get; set; }
        public string TransId { get; set; }
        public string Surname { get; set; }
        public string Othernames { get; set; }
        public string MaidenName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public Decimal Premium { get; set; }
    }
}