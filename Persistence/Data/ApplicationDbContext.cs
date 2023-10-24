using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Http;
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
        private readonly IHttpContextAccessor accessor;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor accessor) : base(options)
        {
            this.accessor = accessor;
        }

        public DbSet<Audit> Audits { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Vendor> Vendors { get; set; }
        public DbSet<Plan> Plans { get; set; }
        public DbSet<OneTimePassword> OneTimePasswords { get; set; }
        public DbSet<ErrorLog> ErrorLogs { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<PlanDescription> PlanDescriptions { get; set; }

        public virtual async Task<int> SaveChangesAsync(string userId = null)
        {
            OnBeforeSaveChanges(userId);
            var result = await base.SaveChangesAsync();
            return result;
        }

        private void OnBeforeSaveChanges(string userId)
        {
            ChangeTracker.DetectChanges();

            var auditEntries = new List<AuditEntry>();
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;
                var auditEntry = new AuditEntry(entry);
                auditEntry.TableName = entry.Entity.GetType().Name;
                auditEntry.UserId = accessor.HttpContext == null ? null : accessor.HttpContext.User?.Claims?.FirstOrDefault(x => x.Type == "UserId")?.Value;
                auditEntries.Add(auditEntry);
                foreach (var property in entry.Properties)
                {
                    string propertyName = property.Metadata.Name;
                    if (property.Metadata.IsPrimaryKey())
                    {
                        auditEntry.KeyValues[propertyName] = property.CurrentValue;
                        continue;
                    }
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            auditEntry.AuditType = AuditType.Create;
                            auditEntry.NewValues[propertyName] = property.CurrentValue;
                            break;
                        case EntityState.Deleted:
                            auditEntry.AuditType = AuditType.Delete;
                            auditEntry.OldValues[propertyName] = property.OriginalValue;
                            break;
                        case EntityState.Modified:
                            if (property.IsModified)
                            {
                                auditEntry.ChangedColumns.Add(propertyName);
                                auditEntry.AuditType = AuditType.Update;
                                auditEntry.OldValues[propertyName] = property.OriginalValue;
                                auditEntry.NewValues[propertyName] = property.CurrentValue;
                            }
                            break;
                    }
                }
            }
            foreach (var auditEntry in auditEntries)
            {
                Audits.Add(auditEntry.ToAudit());
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            //builder.Entity<Subscription>()
            // .HasKey(bc => new { bc.ApplicationUserId, bc.ProductId });

            //builder.Entity<Subscription>()
            //    .HasOne(bc => bc.ApplicationUser)
            //    .WithMany(b => b.Subscriptions)
            //    .HasForeignKey(bc => bc.ApplicationUserId);

            //builder.Entity<Subscription>()
            //    .HasOne(bc => bc.Product)
            //    .WithMany(c => c.Subscriptions)
            //    .HasForeignKey(bc => bc.ProductId);
        }
    }
}
