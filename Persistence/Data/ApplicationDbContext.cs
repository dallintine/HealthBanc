using Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, long>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Plan> Plans { get; set; }
        public DbSet<OneTimePassword> OneTimePasswords { get; set; }
        //public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.HasDefaultSchema("HealthBanc");

            builder.Entity<Subscription>()
             .HasKey(bc => new { bc.ApplicationUserId, bc.ProductId });

            builder.Entity<Subscription>()
                .HasOne(bc => bc.ApplicationUser)
                .WithMany(b => b.Subscriptions)
                .HasForeignKey(bc => bc.ApplicationUserId);

            builder.Entity<Subscription>()
                .HasOne(bc => bc.Product)
                .WithMany(c => c.Subscriptions)
                .HasForeignKey(bc => bc.ProductId);
        }
    }
}
