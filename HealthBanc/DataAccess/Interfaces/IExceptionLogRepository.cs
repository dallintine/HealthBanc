using HealthBanc.DataAccess.Implementation;
using HealthBanc.Domain.Models.ExceptionLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.DataAccess.Interfaces
{
    public interface IExceptionLogRepository : IBaseRepository<ExceptionLog>
    {
    }
}
