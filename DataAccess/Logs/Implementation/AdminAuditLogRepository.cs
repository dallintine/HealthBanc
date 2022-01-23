using DataAccess.DTO.AuditDTO;
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

        public async Task<PagedResponse<AdminAuditLogDTO>> GetPaginatedAdminActivityLog(PaginationQuery paginationQuery,string channel)
        {
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

            var newQueryable = queryable.Skip(skip).Take(paginationQuery.PageSize).Select(x => new AdminAuditLogDTO {Date = x.Date, ActionApplied = x.ActionApplied });
            var recordCount = await queryable.CountAsync();
            var paginatedResponse = new PagedResponse<AdminAuditLogDTO>
            {
                Data = await newQueryable.ToListAsync(),
                PageNumber = paginationQuery.PageNumber >= 1 ? paginationQuery.PageNumber : (int?)null,
                PageSize = paginationQuery.PageSize >= 1 ? paginationQuery.PageSize : (int?)null,
                RecordCount = recordCount,
                PageCount = Convert.ToInt32(Math.Ceiling((double)recordCount / (double)paginationQuery.PageSize))
            };
            return paginatedResponse;
        }
    }
}
