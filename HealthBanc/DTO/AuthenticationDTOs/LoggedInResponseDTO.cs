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
        public DateTime ExpiryTime { get; set; }
        public IEnumerable<string> Roles { get; set; }
    }
}
