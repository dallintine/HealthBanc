using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.DTO.AuthenticationDTOs
{
    public class LoggedInResponseDTO
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public string Username { get; set; }
        public string Name { get; set; }
        public bool Success { get; set; }
        public DateTime ExpiryTime { get; set; }
        public IEnumerable<string> Roles { get; set; }
        public IEnumerable<string> Errors { get; set; }
        public List<string> Services { get; set; }
    }

    public class LoggedInAdminResponseDTO
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public bool Success { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public DateTime ExpiryTime { get; set; }
        public IEnumerable<string> Roles { get; set; }
        public IEnumerable<string> Errors { get; set; }
    }
}
