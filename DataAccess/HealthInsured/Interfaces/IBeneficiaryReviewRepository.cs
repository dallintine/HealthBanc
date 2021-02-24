using DataAccess.General.Interfaces;
using Domain.Models.Axa.Hygeia_Insurance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured.Interfaces
{
    public interface IBeneficiaryReviewRepository : IBaseRepository<BeneficiaryReviewUser>
    {
        Task<PagedResponse<BeneficiaryReviewUser>> FilterBeneficiariesReview(PaginationQuery paginationQuery, int companyProfileId);
        Task<BeneficiaryReviewUser> GetByEmail(string email);
        Task<IQueryable<BeneficiaryReviewUser>> QueryableBeneficiaryReviews(int userId);
    }
}
