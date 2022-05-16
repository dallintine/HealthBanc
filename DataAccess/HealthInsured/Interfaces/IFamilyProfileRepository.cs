using DataAccess.General.Interfaces;
using Domain.Models.Axa_Hygeia_Insurance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured.Interfaces
{
    public interface IFamilyProfileRepository : IBaseRepository<FamilyProfile>
    {
        Task<FamilyProfile> GetByEmail(string email);
        Task<FamilyProfile> GetByUserId(int userId);
        Task<FamilyProfile> GetFamilyByFamilyId(int familyId);
        Task<FamilyProfile> GetExtendedFamilyDetails(int userId);
        Task<PagedResponse<InsuranceUserProfile>> PaginatedFamilyMembers(PaginationQuery paginationQuery, int userId);
        Task<FamilyProfile> GetExtendedFamilyDetailsById(int familyId);
    }
}
