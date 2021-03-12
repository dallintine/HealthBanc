using DataAccess.General.Interfaces;
using Domain.Models.Axa.Hygeia_Insurance;
using Domain.Models.ReportAndLogs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Logs.Interfaces
{
    public interface IActivityLogRepository : IBaseRepository<ActivityLog>
    {
        Task<PagedResponse<ActivityLog>> GetPaginatedActivityLogByProfileId(PaginationQuery paginationQuery, int? individualProfileId, int? companyProfileId,string service);
    }
}
