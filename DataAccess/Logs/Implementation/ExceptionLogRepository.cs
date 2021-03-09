using DataAccess.General.Implementation;
using DataAccess.HealthInsured.Interfaces;
using DataAccess.Logs.Interfaces;
using Domain.Models.ExceptionLog;
using Microsoft.EntityFrameworkCore;
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

        public async Task<PagedResponse<ExceptionLog>> GetPaginatedErrorLog(PaginationQuery paginationQuery)
        {
            var paginatedResponse = new PagedResponse<ExceptionLog>();
            var queryable = _context.ExceptionLogs.AsQueryable();

            //Sort the users
            queryable =  queryable.OrderByDescending(s => s.ErrorDate);


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
