using DataAccess.General.Implementation;
using DataAccess.HealthInsured.Interfaces;
using DataAccess.Logs.Interfaces;
using Domain.Models.ReportAndLogs;
using Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Logs.Implementation
{
    public class UserAuditLogRepository : BaseRepository<UserAuditLog>, IUserAuditLogRepository
    {
        public UserAuditLogRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
