using HealthBanc.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<ClassOrRole> ClassOrRoles { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<IdentityRole>().HasData(
                 new { Id = "1", Name = "SuperAdmin", NormalizedName = "SUPERADMIN" },
                 new { Id = "2", Name = "RepSuperAdmin", NormalizedName = "REPSUPERADMIN"},
                 new { Id = "3", Name = "Initiator", NormalizedName = "INITIATOR" },
                 new { Id = "4", Name = "Reviewer", NormalizedName = "REVIEWER" },
                 new { Id = "5", Name = "Authorizer", NormalizedName = "Authorizer" }
            );

            builder.Entity<ClassOrRole>().HasData(
               new { Id = 1, Name = "SuperAdmin" },
               new { Id = 2, Name = "Initiator" },
               new { Id = 3, Name = "Reviewer" },
               new { Id = 4, Name = "Authorizer" }
           );
        }
    }
}
