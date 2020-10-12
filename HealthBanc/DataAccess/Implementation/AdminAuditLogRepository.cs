using HealthBanc.Data;
using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Domain.Models.ReportAndLogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.DataAccess.Implementation
{
    public class AdminAuditLogRepository : BaseRepository<AdminAuditLog>, IAdminAuditLogRepository
    {
        public AdminAuditLogRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
