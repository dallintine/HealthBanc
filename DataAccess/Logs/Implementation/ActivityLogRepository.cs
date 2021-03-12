using DataAccess.General.Implementation;
using DataAccess.General.Interfaces;
using DataAccess.HealthInsured.Interfaces;
using DataAccess.Logs.Interfaces;
using Domain.Models.Axa.Hygeia_Insurance;
using Domain.Models.ReportAndLogs;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Logs.Implementation
{
    public class ActivityLogRepository : BaseRepository<ActivityLog>, IActivityLogRepository
    {
        public ActivityLogRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<PagedResponse<ActivityLog>> GetPaginatedActivityLogByProfileId(PaginationQuery paginationQuery, int? individualProfileId, int? companyProfileId,string service)
        {
            var paginatedResponse = new PagedResponse<ActivityLog>();
            var queryable = _context.ActivityLogs.Where(x => x.Service == service).AsQueryable();
            if (!(individualProfileId is null))
            {
                queryable = queryable.Where(x => x.InsuranceUserProfileId == individualProfileId).AsQueryable();
            }
            else
            {
                queryable = queryable.Where(x => x.CompanyProfileId == companyProfileId).AsQueryable();
            }
            var skip = (paginationQuery.PageNumber - 1) * paginationQuery.PageSize;

            var newQueryable = queryable.Skip(skip).Take(paginationQuery.PageSize).AsQueryable();
            paginatedResponse.Data = await newQueryable.ToListAsync();
            var recordCount2 = await queryable.CountAsync();
            paginatedResponse.RecordCount = recordCount2;
            paginatedResponse.PageCount = Convert.ToInt32(Math.Ceiling((double)recordCount2 / (double)paginationQuery.PageSize));
            return paginatedResponse;
        }
    }
}
