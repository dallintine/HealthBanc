using Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.General.Interfaces
{
    public interface IHealthFinanceRepository : IBaseRepository<HealthFinance>
    {
        Task<PagedResponse<HealthFinance>> GetPaginatedFinanceData(PaginationQuery paginationQuery);
    }
}
