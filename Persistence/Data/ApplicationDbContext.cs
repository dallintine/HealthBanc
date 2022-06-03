using Domain.Models;
using Domain.Models.Axa.Hygeia_Insurance;
using Domain.Models.Axa_Hygeia_Insurance;
using Domain.Models.ExceptionLog;
using Domain.Models.ReportAndLogs;
using Domain.Models.Wallet;
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
        public DbSet<InsuranceUserProfile> InsuranceUserProfiles { get; set; }
        public DbSet<InsuranceCompletionProfile> InsuranceCompletionProfiles { get; set; }
        public DbSet<DebitCard> Cards { get; set; }
        public DbSet<PaymentReference> PaymentReferences { get; set; }
        public DbSet<UserAuditLog> UserAuditLogs { get; set; }
        public DbSet<AdminAuditLog> AdminAuditLogs { get; set; }
        public DbSet<ExceptionLog> ExceptionLogs { get; set; }
        public DbSet<AdminLogin_LogoutLog> AuditLogin_LogoutLogs { get; set; }
        public DbSet<UserLogin_LogoutLog> UserLogin_LogoutLogs { get; set; }
        public DbSet<PasswordChangeHistory> PasswordChangeHistories { get; set; }
        public DbSet<ScheduledPayment> ScheduledPayments { get; set; }
        public DbSet<ScheduledEnrollment> ScheduledEnrollments { get; set; }
        public DbSet<EnrollmentOnReactivation> EnrollmentOnReactivations { get; set; }
        public DbSet<PaymentOnReactivation> PaymentOnReactivations { get; set; }
        public DbSet<AxaMansardHospitalList> AxaMansardHospitalLists { get; set;}
        public DbSet<HygeiaHospitalList> HygeiaHospitalLists { get; set; }
        public DbSet<EnrollmentOnOnboarding> EnrollmentOnOnboardings { get; set; }
        public DbSet<CompanyProfile> CompanyProfiles { get; set; }
        public DbSet<FamilyProfile> FamilyProfiles { get; set; }
        public DbSet<BeneficiaryReviewUser> BeneficiaryReviewUsers { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<EncryptedAcessToken> EncryptedAcessTokens { get; set; }
        public DbSet<HealthFinance> HealthFinances { get; set; }
        public DbSet<HMOPayment> HMOPayments { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<OtpValidation> OtpValidations { get; set; }
        public DbSet<UserWallet> Wallets { get; set; }


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
