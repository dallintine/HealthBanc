using DataAccess.General.Implementation;
using DataAccess.HealthInsured.Interfaces;
using Domain.Models.Axa_Hygeia_Insurance;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured.Implementation
{
    public class FamilyProfileRepository : BaseRepository<FamilyProfile>, IFamilyProfileRepository
    {
        public FamilyProfileRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<FamilyProfile> GetByUserId(int userId)
        {
            return await _context.FamilyProfiles.Include(x => x.Cards).FirstOrDefaultAsync(x => x.UserId == userId);
        }

        public async Task<FamilyProfile> GetExtendedFamilyDetails(int userId)
        {
            return await _context.FamilyProfiles.Include(x => x.Cards).Include(x => x.InsuranceUserProfiles).FirstOrDefaultAsync(x => x.UserId == userId);
        }

        public async Task<FamilyProfile> GetByEmail(string email)
        {
            return await _context.FamilyProfiles.Include(x => x.Cards).FirstOrDefaultAsync(x => x.Email == email);
        }

        public async Task<FamilyProfile> GetFamilyByFamilyId(int familyId)
        {
            var family = await _context.FamilyProfiles.Include(x => x.Cards).FirstOrDefaultAsync(x => x.Id == familyId);
            return family;
        }

        public async Task<PagedResponse<InsuranceUserProfile>> PaginatedFamilyMembers(PaginationQuery paginationQuery,int userId)
        {
            var paginatedResponse = new PagedResponse<InsuranceUserProfile>();
            var familyProfile = await _context.FamilyProfiles.Include(x => x.InsuranceUserProfiles).FirstOrDefaultAsync(x => x.UserId == userId);
            if(familyProfile is null)
            {
                return new PagedResponse<InsuranceUserProfile>();
            }
            var queryable = familyProfile.InsuranceUserProfiles.AsQueryable();

            if (!string.IsNullOrEmpty(paginationQuery.SearchText))
            {
                queryable = queryable.Where(x => x.Surname.Contains(paginationQuery.SearchText) || x.Othernames.Contains(paginationQuery.SearchText));
            }

            //Sort the users
            queryable = paginationQuery.SortBy == 1 ? queryable.OrderBy(s => s.Surname) : queryable.OrderBy(s => s.Othernames) ;

            var skip = (paginationQuery.PageNumber - 1) * paginationQuery.PageSize;

            var newQueryable = queryable.Skip(skip).Take(paginationQuery.PageSize).AsQueryable();
            paginatedResponse.Data = newQueryable.ToList();
            var recordCount = queryable.Count();
            paginatedResponse.RecordCount = recordCount;
            paginatedResponse.PageNumber = paginationQuery.PageNumber >= 1 ? paginationQuery.PageNumber : (int?)null;
            paginatedResponse.PageSize = paginationQuery.PageSize >= 1 ? paginationQuery.PageSize : (int?)null;
            paginatedResponse.PageCount = Convert.ToInt32(Math.Ceiling((double)recordCount / (double)paginationQuery.PageSize));
            return paginatedResponse;
        }
    }
}
