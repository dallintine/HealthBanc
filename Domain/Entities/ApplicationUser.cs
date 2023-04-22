using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class ApplicationUser : IdentityUser<long>
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime LastLoginDate {get;set;}
        public DateTime RefreshTokenExpiryTime { get; set; }
        public string RefreshToken { get; set; }
        public List<Subscription> Subscriptions { get; set; }
    }
}
