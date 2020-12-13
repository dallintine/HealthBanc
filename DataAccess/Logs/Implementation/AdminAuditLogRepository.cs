using DataAccess.General.Implementation;
using DataAccess.HealthInsured_AxaMansard.Interfaces;
using DataAccess.Logs.Interfaces;
using Domain.Models.ReportAndLogs;
using Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Logs.Implementation
{
    public class AdminAuditLogRepository : BaseRepository<AdminAuditLog>, IAdminAuditLogRepository
    {
        public AdminAuditLogRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
