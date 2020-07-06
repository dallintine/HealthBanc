using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.DTO.AuthenticationDTOs
{
    public class LoggedInResponseDTO
    {
        public string Token { get; set; }
        public string Username { get; set; }
        public string Name { get; set; }
        public string CompanyName { get; set; }
        public DateTime ExpiryTime { get; set; }
        public bool DocumentExist { get; set; }
        public bool ValidIdStatus { get; set; }
        public bool PharmacistLicenceStatus { get; set; }
        public bool PremisesLicenceStatus { get; set; }
        public IEnumerable<string> Roles { get; set; }
        public bool? DocumentStatus { get; set; }
        public string Error { get; set; }
        public string Category { get; set; }
    }
}
