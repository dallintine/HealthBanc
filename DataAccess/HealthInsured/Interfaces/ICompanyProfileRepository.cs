using DataAccess.General.Interfaces;
using Domain.Models.Axa.Hygeia_Insurance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured.Interfaces
{
    public interface ICompanyProfileRepository : IBaseRepository<CompanyProfile>
    {
        Task<CompanyProfile> GetCompanyBeneficiaryReviewUsersByCompanyId(int companyId);
        Task<CompanyProfile> GetCompanyInsuranceUserProfilesByUserId(int userId);
        Task<CompanyProfile> GetCompanyProfileByEmail(string email);
        Task<CompanyProfile> GetCompanyProfileByUserId(int userId);
        IQueryable<CompanyProfile> QueryAllCompanyProfiles();
    }
}
