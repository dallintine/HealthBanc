using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Domain.Models
{
    public class ApplicationUser : IdentityUser<int>
    {
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public DateTime DateOfRegistration { get; set; }
        public bool IsDeleted { get; set; }
        public int? SuperAdminId { get; set; }
        public int? AdminId { get; set; }
        public DateTime LastLoginDate { get; set; }
        public string ServiceUsed { get; set; }
        public string UniqueUsername { get; set; }
        public string RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; }
    }
}
