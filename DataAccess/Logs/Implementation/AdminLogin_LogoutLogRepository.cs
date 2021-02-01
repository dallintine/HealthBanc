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
    public class AdminLogin_LogoutLogRepository : BaseRepository<AdminLogin_LogoutLog>, IAdminLogin_LogoutLogRepository
    {
        public AdminLogin_LogoutLogRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
