using DataAccess.General.Implementation;
using DataAccess.HealthInsured.Interfaces;
using Domain.Models.Axa.Hygeia_Insurance;
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
    public class BeneficiaryReviewRepository : BaseRepository<BeneficiaryReviewUser>, IBeneficiaryReviewRepository
    {
        public BeneficiaryReviewRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<BeneficiaryReviewUser> GetByEmail(string email)
        {
            return await _context.BeneficiaryReviewUsers.FirstOrDefaultAsync(x => x.Email == email);
        }

        public async Task<PagedResponse<BeneficiaryReviewUser>> FilterBeneficiariesReview(PaginationQuery paginationQuery,int companyProfileId)
        {
            var paginatedResponse = new PagedResponse<BeneficiaryReviewUser>();
            var queryable = _context.BeneficiaryReviewUsers.Where(x => x.CompanyProfileId == companyProfileId).AsQueryable();

            if (!string.IsNullOrEmpty(paginationQuery.SearchText))
            {
                queryable = queryable.Where(x => x.FirstName.Contains(paginationQuery.SearchText) || x.LastName.Contains(paginationQuery.SearchText) ||
                x.Email.Contains(paginationQuery.SearchText) || x.PhoneNumber.Contains(paginationQuery.SearchText));
            }
            
            //Sort the users
            queryable = paginationQuery.SortBy == 1 ? queryable.OrderByDescending(s => s.DateCreated) : paginationQuery.SortBy == 2 ? 
                queryable.OrderBy(s => s.FirstName) : paginationQuery.SortBy == 3 ? queryable.OrderBy(s => s.LastName) :
                queryable.OrderByDescending(s => s.Email);

            var skip = (paginationQuery.PageNumber - 1) * paginationQuery.PageSize;

            if (paginationQuery.Filter is null)
            {
                var newQueryable = queryable.Skip(skip).Take(paginationQuery.PageSize).AsQueryable();
                paginatedResponse.Data = await newQueryable.ToListAsync();
                var recordCount = await queryable.CountAsync();
                paginatedResponse.RecordCount = recordCount;
                paginatedResponse.PageCount = Convert.ToInt32(Math.Ceiling((double)recordCount /(double)paginationQuery.PageSize));
                return paginatedResponse;
            }
            else
            {
                //filter users by the restore prop in InusranceUsers
                if (paginationQuery.Filter == 1)
                {
                    var newQueryable = queryable.Where(x => x.IsRemove == false).Skip(skip).Take(paginationQuery.PageSize);
                    paginatedResponse.Data = await newQueryable.ToListAsync();
                    var recordCount = await queryable.CountAsync();
                    paginatedResponse.RecordCount = recordCount;
                    paginatedResponse.PageCount = Convert.ToInt32(Math.Ceiling((double)recordCount / (double)paginationQuery.PageSize));
                    return paginatedResponse;
                }
                else
                {
                    var newQueryable = queryable.Where(x => x.IsRemove == true).Skip(skip).Take(paginationQuery.PageSize);
                    paginatedResponse.Data = await newQueryable.ToListAsync();
                    var recordCount = await queryable.CountAsync();
                    paginatedResponse.RecordCount = recordCount;
                    paginatedResponse.PageCount = Convert.ToInt32(Math.Ceiling((double)recordCount / (double)paginationQuery.PageSize));
                    return paginatedResponse;
                }
            }
        }

        public async Task<IQueryable<BeneficiaryReviewUser>> QueryableBeneficiaryReviews(int userId)
        {
            var beneficiaryReviews = await _context.CompanyProfiles.Include(x => x.BeneficiaryReviewUsers).FirstOrDefaultAsync(x => x.UserId == userId);
            return beneficiaryReviews.BeneficiaryReviewUsers.AsQueryable();
        }
    }
}
