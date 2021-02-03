using DataAccess.General.Interfaces;
using Domain.Models.Axa.Hygeia_Insurance;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured_AxaMansard.Interfaces
{
    public interface ICompanyProfileRepository : IBaseRepository<CompanyProfile>
    {
        Task<CompanyProfile> GetCompanyProfileByEmail(string email);
        Task<CompanyProfile> GetCompanyProfileByUserId(int userId);
    }
}
