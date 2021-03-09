using DataAccess.General.Interfaces;
using Domain.Models.Axa.Hygeia_Insurance;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured.Interfaces
{
    public interface IHealthInsuredActivityLogRepository : IBaseRepository<HealthInsuredActivityLog>
    {
        Task<PagedResponse<HealthInsuredActivityLog>> GetPaginatedActivityLogByProfileId(PaginationQuery paginationQuery, int? individualProfileId, int? companyProfileId);
    }
}
