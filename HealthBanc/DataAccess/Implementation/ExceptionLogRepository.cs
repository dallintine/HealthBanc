using HealthBanc.Data;
using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Domain.Models.ExceptionLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.DataAccess.Implementation
{
    public class ExceptionLogRepository : BaseRepository<ExceptionLog>, IExceptionLogRepository
    {
        public ExceptionLogRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
