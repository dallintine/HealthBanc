using DataAccess.General.Interfaces;
using Domain.Models;
using Domain.Models.Axa_Hygeia_Insurance;
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
        Task<IQueryable<InsuranceUserProfile>> QueryableInsuranceProfilesUnderCompany(int userId);
        Task<PagedResponse<InsuranceUserProfile>> GetPaginatedInsuranceUserProfiles(PaginationQuery paginationQuery);
        Task<InsuranceUserProfile> GetExtendedProfileDetailsByEmail(string email);
        IQueryable<InsuranceUserProfile> QueryAllInsuranceProfiles();
        Task<InsuranceUserProfile> GetByFamilyProfileId(int familyProfileId, int insuranceProfileId);
    }
}
