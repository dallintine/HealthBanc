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
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, AppRole, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<ClassOrRole> ClassOrRoles { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<BackendAdminUser> BackendAdminUsers { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AxaMansardUserProfile> AxaMansardUserProfile { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<AppRole>().HasData(
                 new { Id = 1, Name = "SuperAdmin", NormalizedName = "SUPERADMIN" },
                 new { Id = 2, Name = "RepSuperAdmin", NormalizedName = "REPSUPERADMIN"},
                 new { Id = 3, Name = "Initiator", NormalizedName = "INITIATOR" },
                 new { Id = 4, Name = "Reviewer", NormalizedName = "REVIEWER" },
                 new { Id = 5, Name = "Authorizer", NormalizedName = "Authorizer" },
                 new { Id = 6, Name = "Super-Administrator", NormalizedName = "SUPER-ADMINISTRATOR" },
                 new { Id = 7, Name = "Administrator", NormalizedName = "ADMINISTRATOR" },
                 new { Id = 8, Name = "Technical-Support", NormalizedName = "TECHNICAL-SUPPORT" },
                 new { Id = 9, Name = "Analyst", NormalizedName = "ANALYST" }
            );

            builder.Entity<ClassOrRole>().HasData(
               new { Id = 1, Name = "SuperAdmin" },
               new { Id = 2, Name = "RepSuperAdmin" },
               new { Id = 3, Name = "Initiator" },
               new { Id = 4, Name = "Reviewer" },
               new { Id = 5, Name = "Authorizer" },
               new { Id = 6, Name = "Super-Administrator"},
               new { Id = 7, Name = "Administrator"},
               new { Id = 8, Name = "Technical-Support"},
               new { Id = 9, Name = "Analyst"}
           );

            builder.Entity<Service>().HasData(
                new { Id=1, Name="Pharmmall"},
                new {Id = 2, Name="Insurance"}
           );
        }
    }
}
