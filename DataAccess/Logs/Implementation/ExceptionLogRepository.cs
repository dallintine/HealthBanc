using DataAccess.General.Implementation;
using DataAccess.HealthInsured_AxaMansard.Interfaces;
using DataAccess.Logs.Interfaces;
using Domain.Models.ExceptionLog;
using Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Logs.Implementation
{
    public class ExceptionLogRepository : BaseRepository<ExceptionLog>, IExceptionLogRepository
    {
        public ExceptionLogRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
