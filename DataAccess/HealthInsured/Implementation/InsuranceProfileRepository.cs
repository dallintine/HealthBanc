using DataAccess.General.Implementation;
using DataAccess.HealthInsured.Interfaces;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured.Implementation
{
    public class InsuranceProfileRepository : BaseRepository<InsuranceUserProfile>, IInsuranceProfileRepository
    {
        public InsuranceProfileRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<InsuranceUserProfile> GetByUserIdAsync(int id)
        {
            return await _context.InsuranceUserProfiles.Include(x => x.Cards).FirstOrDefaultAsync(x => x.UserId == id);
        }

        public async Task<InsuranceUserProfile> GetByEmail(string email)
        {
            return await _context.InsuranceUserProfiles.Include(x => x.Cards).FirstOrDefaultAsync(x => x.Email == email);
        }

        public async Task<InsuranceUserProfile> GetByIdAsync(int id)
        {
            return await _context.InsuranceUserProfiles.Include(x => x.Cards).FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<PagedResponse<InsuranceUserProfile>> GetAllInsuranceProfileUnderCompany(PaginationQuery paginationQuery,int CompanyProfileId)
        {
            var paginatedResponse = new PagedResponse<InsuranceUserProfile>();
            var queryable = _context.InsuranceUserProfiles.Where(x => x.CompanyProfileId == CompanyProfileId).AsQueryable();

            if (paginationQuery is null)
            {
                paginatedResponse.Data = await queryable.ToListAsync();
                var recordCount = await queryable.CountAsync();
                paginatedResponse.RecordCount = recordCount;
                paginatedResponse.PageCount = Convert.ToInt32(Math.Ceiling((double)recordCount / (double)paginationQuery.PageSize));
                return paginatedResponse;
            }
            //If Status is null returns all users
            //if status is 1 returns active users
            //if status is 2 returns Inactive users
            if (paginationQuery.Status != null)
            {
                if (paginationQuery.Status == 1) queryable = queryable.Where(x => x.ActiveStatus == true).AsQueryable();
                if (paginationQuery.Status == 2) queryable = queryable.Where(x => x.SubscriptionStatus == false && x.ActiveStatus == false).AsQueryable();
            }

            if (!string.IsNullOrEmpty(paginationQuery.SearchText))
            {
                queryable = queryable.Where(x => x.Othernames.Contains(paginationQuery.SearchText) || x.Surname.Contains(paginationQuery.SearchText) ||
                x.Email.Contains(paginationQuery.SearchText));
            }

            //Sort the users
            queryable = paginationQuery.SortBy == 1 ? queryable.OrderBy(s => s.Othernames) : paginationQuery.SortBy == 2 ? queryable.OrderBy(s => s.Surname) :
                paginationQuery.SortBy == 3 ? queryable.OrderBy(s => s.Email) : queryable.OrderByDescending(s => s.DateCreated);

            var skip = (paginationQuery.PageNumber - 1) * paginationQuery.PageSize;

            var newQueryable = queryable.Skip(skip).Take(paginationQuery.PageSize).AsQueryable();
            paginatedResponse.Data = await newQueryable.ToListAsync();
            var recordCount2 = await queryable.CountAsync();
            paginatedResponse.RecordCount = recordCount2;
            paginatedResponse.PageCount = Convert.ToInt32(Math.Ceiling((double)recordCount2 / (double)paginationQuery.PageSize));
            return paginatedResponse;
        }

        public async Task<IQueryable<InsuranceUserProfile>> QueryableCompanyProfile(int userId)
        {
            var companyProfile = await  _context.CompanyProfiles.Include(x => x.InsuranceUserProfiles).FirstOrDefaultAsync(x => x.UserId == userId);
            return companyProfile.InsuranceUserProfiles.AsQueryable();
        }
    }
}
