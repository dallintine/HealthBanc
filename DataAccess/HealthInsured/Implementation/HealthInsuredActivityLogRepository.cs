using DataAccess.General.Implementation;
using DataAccess.HealthInsured.Interfaces;
using Domain.Models.Axa.Hygeia_Insurance;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured.Implementation
{
    public class HealthInsuredActivityLogRepository : BaseRepository<HealthInsuredActivityLog>, IHealthInsuredActivityLogRepository
    {
        public HealthInsuredActivityLogRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<PagedResponse<HealthInsuredActivityLog>> GetPaginatedActivityLogByProfileId(PaginationQuery paginationQuery, int? individualProfileId, int? companyProfileId)
        {
            var paginatedResponse = new PagedResponse<HealthInsuredActivityLog>();
            var queryable = _context.HealthInsuredActivityLogs.AsQueryable();
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
