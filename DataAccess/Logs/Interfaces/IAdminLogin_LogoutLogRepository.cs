using DataAccess.General.Interfaces;
using Domain.Models.ReportAndLogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Logs.Interfaces
{
    public interface IAdminLogin_LogoutLogRepository : IBaseRepository<AdminLogin_LogoutLog>
    {
    }
}
