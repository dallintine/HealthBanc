using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.General.Interfaces
{
    public interface IHealthFinanceRepository : IBaseRepository<HealthFinance>
    {
        Task<HealthFinance> GetByEmail(string email);
        Task<PagedResponse<HealthFinance>> GetPaginatedFinanceData(PaginationQuery paginationQuery, DateTime? startDate, DateTime? endDate);
        IQueryable<HealthFinance> QueryFinanceData();
    }
}
