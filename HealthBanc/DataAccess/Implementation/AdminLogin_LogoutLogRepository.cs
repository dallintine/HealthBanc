using HealthBanc.Data;
using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Domain.Models.ReportAndLogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.DataAccess.Implementation
{
    public class AdminLogin_LogoutLogRepository : BaseRepository<AdminLogin_LogoutLog>, IAdminLogin_LogoutLogRepository
    {
        public AdminLogin_LogoutLogRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
