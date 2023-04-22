using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Identity
{
    public class LoginResponse
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public DateTime ExpiryTime { get; set; }
        public IEnumerable<string> Roles { get; set; }
        public IEnumerable<string> Errors { get; set; }
    }
}
