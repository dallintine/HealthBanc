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
        public string UniqueUsername { get; set; }
        public string LastName { get; set; }
        public string OAuthSubject { get; set; }
        public string Address { get; set; }
        public DateTime LastLoginDate {get;set;}
        public DateTime RefreshTokenExpiryTime { get; set; }
        public string RefreshToken { get; set; }
        public List<Subscription> Subscriptions { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}
