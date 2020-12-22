using Domain.Models;
using Domain.Models.AxaMansard_Insurance;
using Domain.Models.ExceptionLog;
using Domain.Models.ReportAndLogs;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Persistence
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
        public DbSet<AxaMansardCompletionProfile> AxaMansardCompletionProfiles { get; set; }
        public DbSet<DebitCard> Cards { get; set; }
        public DbSet<PaymentReference> PaymentReferences { get; set; }
        public DbSet<UserAuditLog> UserAuditLogs { get; set; }
        public DbSet<AdminAuditLog> AdminAuditLogs { get; set; }
        public DbSet<ExceptionLog> ExceptionLogs { get; set; }
        public DbSet<AdminLogin_LogoutLog> AuditLogin_LogoutLogs { get; set; }
        public DbSet<UserLogin_LogoutLog> UserLogin_LogoutLogs { get; set; }
        public DbSet<PasswordChangeHistory> PasswordChangeHistories { get; set; }
        public DbSet<InsufficientChargeTransaction> InsufficientChargeTransactions { get; set; }
        public DbSet<ScheduledPayment> ScheduledPayments { get; set; }
        public DbSet<ScheduledAxaEnrollment> ScheduledAxaEnrollments { get; set; }
        public DbSet<AxaEnrollmentOnReactivation> AxaEnrollmentReactivations { get; set; }
        public DbSet<PaymentOnReactivation> PaymentOnReactivations { get; set; }
        public DbSet<AxaMansardHospitalList> AxaMansardHospitalLists { get; set;}
        public DbSet<AxaEnrollmentOnOnboarding> AxaEnrollmentOnOnboardings { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<AppRole>().HasData(
                 new { Id = 1, Name = "SuperAdmin", NormalizedName = "SUPERADMIN" },
                 new { Id = 6, Name = "Super-Administrator", NormalizedName = "SUPER-ADMINISTRATOR" },
                 new { Id = 7, Name = "Administrator", NormalizedName = "ADMINISTRATOR" },
                 new { Id = 8, Name = "Technical-Support", NormalizedName = "TECHNICAL-SUPPORT" },
                 new { Id = 9, Name = "Analyst", NormalizedName = "ANALYST" }
            );

            builder.Entity<ClassOrRole>().HasData(
               new { Id = 1, Name = "SuperAdmin" },
               new { Id = 6, Name = "Super-Administrator"},
               new { Id = 7, Name = "Administrator"},
               new { Id = 8, Name = "Technical-Support"},
               new { Id = 9, Name = "Analyst"}
           );

            builder.Entity<Service>().HasData(
                new { Id=1, Name="HealthMall"},
                new {Id = 2, Name="HealthInsured"}
           );
        }
    }
}
