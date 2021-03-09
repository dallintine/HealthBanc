using DataAccess.General.Interfaces;
using Domain.Models.ExceptionLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Logs.Interfaces
{
    public interface IExceptionLogRepository : IBaseRepository<ExceptionLog>
    {
        Task<PagedResponse<ExceptionLog>> GetPaginatedErrorLog(PaginationQuery paginationQuery);
    }
}
