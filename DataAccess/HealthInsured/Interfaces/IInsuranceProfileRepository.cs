using DataAccess.General.Interfaces;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured.Interfaces
{
    public interface IInsuranceProfileRepository : IBaseRepository<InsuranceUserProfile>
    {
        Task<InsuranceUserProfile> GetByUserIdAsync(int id);
        Task<InsuranceUserProfile> GetByIdAsync(int id);
        Task<InsuranceUserProfile> GetByEmail(string email);
        Task<PagedResponse<InsuranceUserProfile>> GetAllInsuranceProfileUnderCompany(PaginationQuery paginationQuery, int CompanyProfileId);
        Task<IQueryable<InsuranceUserProfile>> QueryableCompanyProfile(int userId);
        Task<PagedResponse<InsuranceUserProfile>> GetPaginatedInsuranceUserProfiles(PaginationQuery paginationQuery);
    }
}
