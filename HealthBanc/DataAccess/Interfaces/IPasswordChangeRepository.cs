using HealthBanc.DataAccess.Implementation;
using HealthBanc.Domain.Models.ReportAndLogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.DataAccess.Interfaces
{
    public interface IPasswordChangeRepository : IBaseRepository<PasswordChangeHistory>
    {
    }
}
