using DataAccess.General.Interfaces;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.General.Implementation
{
    public class HealthFinanceRepository : BaseRepository<HealthFinance>, IHealthFinanceRepository
    {
        public HealthFinanceRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<PagedResponse<HealthFinance>> GetPaginatedFinanceData(PaginationQuery paginationQuery,DateTime? startDate, DateTime? endDate)
        {
            var paginatedResponse = new PagedResponse<HealthFinance>();
            var queryable = _context.HealthFinances.AsQueryable();

            if (!string.IsNullOrEmpty(paginationQuery.SearchText))
            {
                queryable = queryable.Where(x => x.Email.Contains(paginationQuery.SearchText) || x.BusinessName.Contains(paginationQuery.SearchText) ||
                x.Name.Contains(paginationQuery.SearchText) || x.Phonenumber.Contains(paginationQuery.SearchText));
            }

            //Sort the users
            queryable = paginationQuery.SortBy == 1 ? queryable.OrderBy(s => s.Email) : paginationQuery.SortBy == 2 ? queryable.OrderBy(s => s.BusinessName) :
                paginationQuery.SortBy == 3 ? queryable.OrderBy(s => s.Name) : paginationQuery.SortBy == 4 ? queryable.OrderByDescending(s => s.Amount) : queryable.OrderByDescending(s => s.DateSubmitted);

            //Filter by date

            if(startDate != null)
            {
                queryable = queryable.Where(x => x.DateSubmitted.Date >= startDate.Value.Date);
            }

            if (endDate != null)
            {
                queryable = queryable.Where(x => x.DateSubmitted.Date <= endDate.Value.Date.Date);
            }

            var skip = (paginationQuery.PageNumber - 1) * paginationQuery.PageSize;

            var newQueryable = queryable.Skip(skip).Take(paginationQuery.PageSize).AsQueryable();
            paginatedResponse.Data = await newQueryable.ToListAsync();
            var recordCount = await queryable.CountAsync();
            paginatedResponse.RecordCount = recordCount;
            paginatedResponse.PageCount = Convert.ToInt32(Math.Ceiling((double)recordCount / (double)paginationQuery.PageSize));
            return paginatedResponse;
        }

        public IQueryable<HealthFinance> QueryFinanceData()
        {
            var data = _context.HealthFinances.AsQueryable();
            return data;
        }

        public async Task<HealthFinance> GetByEmail(string email)
        {
            return await _context.HealthFinances.FirstOrDefaultAsync(x => x.Email == email);
        }
    }
}
