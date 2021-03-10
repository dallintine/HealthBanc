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
    public class PaymentReferenceRepository : BaseRepository<PaymentReference>, IPaymentReferenceRepository
    {
        public PaymentReferenceRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<PaymentReference> GetByUserId(int userId)
        {
            return await _context.PaymentReferences.FirstOrDefaultAsync(x => x.UserId == userId);
        }
        public async Task<PaymentReference> GetById(int id)
        {
            return await _context.PaymentReferences.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<PaymentReference> GetByReference(string reference)
        {
            return await _context.PaymentReferences.FirstOrDefaultAsync(x => x.Refernce == reference);
        }

        public async Task<PagedResponse<PaymentReference>> GetPaginatedPaymentReference(PaginationQuery paginationQuery,string email)
        {
            var paginatedResponse = new PagedResponse<PaymentReference>();
            var queryable = _context.PaymentReferences.Where(x => x.InsuranceUserProfileId != null).Include(x => x.InsuranceUserProfile).AsQueryable();

            if (email != null) queryable = queryable.Where(x => x.InsuranceUserProfile.Email == email);

            //If Status is null returns all users
            //if status is 1 returns successful transactions
            //if status is 2 returns abandoned transactions
            //if status is 3 returns failed transactions
            if (paginationQuery.Status != null)
            {
                if (paginationQuery.Status == 1) queryable = queryable.Where(x => x.Status.ToLower().Trim() == "successfully").AsQueryable();
                if (paginationQuery.Status == 2) queryable = queryable.Where(x => x.Status.ToLower().Trim() != "successfully" && x.Status.ToLower().Trim() != "failed").AsQueryable();
                if (paginationQuery.Status == 3) queryable = queryable.Where(x => x.Status.ToLower().Trim() == "failed").AsQueryable();
            }

            if (!string.IsNullOrEmpty(paginationQuery.SearchText))
            {
                queryable = queryable.Where(x => x.InsuranceUserProfile.Othernames.Contains(paginationQuery.SearchText) || x.InsuranceUserProfile.Surname.Contains(paginationQuery.SearchText) ||
                x.InsuranceUserProfile.Email.Contains(paginationQuery.SearchText));
            }

            //Sort the users
            queryable = paginationQuery.SortBy == 1 ? queryable.OrderBy(s => s.InsuranceUserProfile.Othernames) : paginationQuery.SortBy == 2 ? queryable.OrderBy(s => s.InsuranceUserProfile.Surname) :
                paginationQuery.SortBy == 3 ? queryable.OrderBy(s => s.InsuranceUserProfile.Email) : queryable.OrderByDescending(s => s.Date);

            //If filter is 1 filter out for hygeia
            // If filer is not 1 filter out for axamansard
            if (!(paginationQuery.Filter is null))
            {
                queryable = paginationQuery.Filter is 1 ? queryable.Where(x => x.InsuranceUserProfile.InsuranceService.Contains("hygeia")) : queryable.Where(x => !(x.InsuranceUserProfile.InsuranceService.Contains("hygeia")));
            }

            var skip = (paginationQuery.PageNumber - 1) * paginationQuery.PageSize;

            var newQueryable = queryable.Skip(skip).Take(paginationQuery.PageSize).AsQueryable();
            paginatedResponse.Data = await newQueryable.ToListAsync();
            var recordCount2 = await queryable.CountAsync();
            paginatedResponse.RecordCount = recordCount2;
            paginatedResponse.PageCount = Convert.ToInt32(Math.Ceiling((double)recordCount2 / (double)paginationQuery.PageSize));
            return paginatedResponse;
        }

        public IQueryable<PaymentReference> QueryAllPaymentReference()
        {
            return _context.PaymentReferences.AsQueryable();
        }
    }
}
