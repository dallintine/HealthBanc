using DataAccess.DTO.InsuranceDTO;
using DataAccess.General.Implementation;
using DataAccess.HealthInsured.Interfaces;
using Domain.Models;
using Domain.Models.Axa_Hygeia_Insurance;
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

        public async Task<InsuranceUserProfile> GetByEnrolleNumber(string enrollee)
        {
            return await _context.InsuranceUserProfiles.FirstOrDefaultAsync(x => x.TransId == enrollee);
        }

        public async Task<InsuranceUserProfile> GetByEmail(string email)
        {
            return await _context.InsuranceUserProfiles.Include(x => x.Cards).FirstOrDefaultAsync(x => x.Email == email);
        }

        public async Task<InsuranceUserProfile> GetExtendedProfileDetailsByEmail(string email)
        {
            return await _context.InsuranceUserProfiles.Where(x => x.Email == email).Include(x => x.Cards)/*.Include(x => x.PaymentReferences)*/.Include(x => x.HealthInsuredActivityLogs).FirstOrDefaultAsync();
        }

        public async Task<InsuranceUserProfile> GetByIdAsync(int id)
        {
            return await _context.InsuranceUserProfiles.Include(x => x.Cards).FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<InsuranceUserProfile> GetByFamilyProfileId(int familyProfileId, int insuranceProfileId)
        {
            return await _context.InsuranceUserProfiles.FirstOrDefaultAsync(x => x.FamilyProfileId == familyProfileId && x.Id == insuranceProfileId);
        }

        public async Task<PagedResponse<List_IndividualProfileDTO>> GetPaginatedInsuranceUserProfiles(PaginationQuery paginationQuery)
        {
            var queryable = _context.InsuranceUserProfiles.Where(x => x.FamilyProfileId == null).AsQueryable();

            //If Status is null returns all users
            //if status is 1 returns active users
            //if status is 2 returns pending users
            //if status is 3 returns inactive users
            if (paginationQuery.Status != null)
            {
                if (paginationQuery.Status == 1) queryable = queryable.Where(x => x.ActiveStatus == true).AsQueryable();
                if (paginationQuery.Status == 2) queryable = queryable.Where(x => x.ActiveStatus == true && x.SubscriptionStatus == false).AsQueryable();
                if (paginationQuery.Status == 3) queryable = queryable.Where(x => x.ActiveStatus == false).AsQueryable();
            }

            if (!string.IsNullOrEmpty(paginationQuery.SearchText))
            {
                queryable = queryable.Where(x => x.Othernames.Contains(paginationQuery.SearchText) || x.Surname.Contains(paginationQuery.SearchText) ||
                x.Email.Contains(paginationQuery.SearchText));
            }

            //Sort the users
            queryable = paginationQuery.SortBy == 1 ? queryable.OrderBy(s => s.Othernames) : paginationQuery.SortBy == 2 ? queryable.OrderBy(s => s.Surname) :
                paginationQuery.SortBy == 3 ? queryable.OrderBy(s => s.Email) : queryable.OrderByDescending(s => s.DateCreated);

            //If filter is 1 filter out for hygeia
            // If filer is not 1 filter out for axamansard
            if (!(paginationQuery.Filter is null))
            {
                queryable = paginationQuery.Filter.Value == 1 ? queryable.Where(x => x.InsuranceService == InsuranceProvider.Hygeia.ToString()).AsQueryable() 
                    : queryable.Where(x => x.InsuranceService == InsuranceProvider.Axamansard.ToString()).AsQueryable();
            }

            var skip = (paginationQuery.PageNumber - 1) * paginationQuery.PageSize;

            var newQueryable = queryable.Skip(skip).Take(paginationQuery.PageSize).Select(x => new List_IndividualProfileDTO {Surname = x.Surname,CompanyName=x.CompanyName
                ,Othernames = x.Othernames,Email =x.Email,PhoneNumber=x.PhoneNumber}).AsQueryable();

            var recordCount = await queryable.CountAsync();
            var paginatedResponse = new PagedResponse<List_IndividualProfileDTO>
            {
                Data = await newQueryable.ToListAsync(),
                PageNumber = paginationQuery.PageNumber >= 1 ? paginationQuery.PageNumber : (int?)null,
                PageSize = paginationQuery.PageSize >= 1 ? paginationQuery.PageSize : (int?)null,
                RecordCount = recordCount,
                PageCount = Convert.ToInt32(Math.Ceiling((double)recordCount / (double)paginationQuery.PageSize))
            };
            return paginatedResponse;
        }

        public async Task<PagedResponse<InsuranceUserProfile>> GetPaginatedReferredInsuranceProfiles(PaginationQuery paginationQuery,int insuranceProfileId)
        {
            var paginatedResponse = new PagedResponse<InsuranceUserProfile>();
            var queryable = _context.InsuranceUserProfiles.Where(x => x.InsurancePayeeId == insuranceProfileId).AsQueryable();

            //If Status is null returns all users
            //if status is 1 returns active users
            //if status is 2 returns pending users
            //if status is 3 returns inactive users
            if (paginationQuery.Status != null)
            {
                if (paginationQuery.Status == 1) queryable = queryable.Where(x => x.ActiveStatus == true).AsQueryable();
                if (paginationQuery.Status == 2) queryable = queryable.Where(x => x.ActiveStatus == true && x.SubscriptionStatus == false).AsQueryable();
                if (paginationQuery.Status == 3) queryable = queryable.Where(x => x.ActiveStatus == false).AsQueryable();
            }

            if (!string.IsNullOrEmpty(paginationQuery.SearchText))
            {
                queryable = queryable.Where(x => x.Othernames.Contains(paginationQuery.SearchText) || x.Surname.Contains(paginationQuery.SearchText) ||
                x.Email.Contains(paginationQuery.SearchText));
            }

            //Sort the users
            queryable = paginationQuery.SortBy == 1 ? queryable.OrderBy(s => s.Othernames) : paginationQuery.SortBy == 2 ? queryable.OrderBy(s => s.Surname) :
                paginationQuery.SortBy == 3 ? queryable.OrderBy(s => s.Email) : queryable.OrderByDescending(s => s.DateCreated);

            //If filter is 1 filter out for hygeia
            // If filer is not 1 filter out for axamansard
            if (!(paginationQuery.Filter is null))
            {
                queryable = paginationQuery.Filter.Value == 1 ? queryable.Where(x => x.InsuranceService == InsuranceProvider.Hygeia.ToString()).AsQueryable()
                    : queryable.Where(x => x.InsuranceService == InsuranceProvider.Axamansard.ToString()).AsQueryable();
            }

            var skip = (paginationQuery.PageNumber - 1) * paginationQuery.PageSize;

            var newQueryable = queryable.Skip(skip).Take(paginationQuery.PageSize).AsQueryable();
            paginatedResponse.Data = await newQueryable.ToListAsync();
            var recordCount2 = await queryable.CountAsync();
            paginatedResponse.RecordCount = recordCount2;
            paginatedResponse.PageCount = Convert.ToInt32(Math.Ceiling((double)recordCount2 / (double)paginationQuery.PageSize));
            return paginatedResponse;
        }

        public async Task<PagedResponse<InsuranceUserProfile>> GetAllInsuranceProfileUnderCompany(PaginationQuery paginationQuery,int CompanyProfileId)
        {
            var paginatedResponse = new PagedResponse<InsuranceUserProfile>();
            var queryable = _context.InsuranceUserProfiles.Where(x => x.CompanyProfileId == CompanyProfileId).AsQueryable();

            //If Status is null returns all users
            //if status is 1 returns active users
            //if status is 2 returns pending users
            //if status is 3 returns inactive users
            if (paginationQuery.Status != null)
            {
                if (paginationQuery.Status == 1) queryable = queryable.Where(x => x.ActiveStatus == true).AsQueryable();
                if (paginationQuery.Status == 2) queryable = queryable.Where(x => x.CompanySubscribedStatus == InsuranceProfile_CompanySubStatusValue.Pending.ToString()).AsQueryable();
                if (paginationQuery.Status == 3) queryable = queryable.Where(x => x.ActiveStatus == false && x.CompanySubscribedStatus == InsuranceProfile_CompanySubStatusValue.Inactive.ToString()).AsQueryable();
            }

            if (!string.IsNullOrEmpty(paginationQuery.SearchText))
            {
                queryable = queryable.Where(x => x.Othernames.Contains(paginationQuery.SearchText) || x.Surname.Contains(paginationQuery.SearchText) ||
                x.Email.Contains(paginationQuery.SearchText));
            }

            //Sort the users
            // 1 = order by firstname
            //2 = order by lastname
            //3 = order  by email
            // 4 = order by date created
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

        public async Task<IQueryable<InsuranceUserProfile>> QueryableInsuranceProfilesUnderCompany(int userId)
        {
            var companyProfile = await  _context.CompanyProfiles.Include(x => x.InsuranceUserProfiles).FirstOrDefaultAsync(x => x.UserId == userId);
            return companyProfile.InsuranceUserProfiles.AsQueryable();
        }

        public IQueryable<InsuranceUserProfile> QueryAllInsuranceProfiles()
        {
            return _context.InsuranceUserProfiles.AsQueryable();
        }
    }
}
