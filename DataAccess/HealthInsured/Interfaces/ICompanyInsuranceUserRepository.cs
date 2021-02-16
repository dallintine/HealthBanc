using DataAccess.General.Interfaces;
using Domain.Models.Axa.Hygeia_Insurance;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured.Interfaces
{
    public interface ICompanyInsuranceUserRepository : IBaseRepository<CompanyInsuranceUser>
    {
        Task<CompanyInsuranceUser> GetByEmail(string email);
    }
}
