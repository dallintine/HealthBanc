using DataAccess.General.Implementation;
using DataAccess.HealthInsured.Interfaces;
using DataAccess.Logs.Interfaces;
using Domain.Models.ReportAndLogs;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Logs.Implementation
{
    public class AdminAuditLogRepository : BaseRepository<AdminAuditLog>, IAdminAuditLogRepository
    {
        public AdminAuditLogRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<PagedResponse<AdminAuditLog>> GetPaginatedAdminActivityLog(PaginationQuery paginationQuery,string channel)
        {
            var paginatedResponse = new PagedResponse<AdminAuditLog>();
            IQueryable<AdminAuditLog> queryable;
            if (channel is null)
            {
                queryable = _context.AdminAuditLogs.AsQueryable();
            }
            else
            {
                queryable = _context.AdminAuditLogs.Where(x => x.Channel == channel).AsQueryable();
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
