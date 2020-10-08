using HealthBanc.Data;
using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Domain.Models.ReportAndLogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.DataAccess.Implementation
{
    public class UserLogin_LogoutLogRepository : BaseRepository<UserLogin_LogoutLog>, IUserLogin_LogoutLogRepository
    {
        public UserLogin_LogoutLogRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
