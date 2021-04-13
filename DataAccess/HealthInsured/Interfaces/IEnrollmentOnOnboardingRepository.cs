using DataAccess.General.Interfaces;
using Domain.Models.Axa.Hygeia_Insurance;
using Domain.Models.Axa_Hygeia_Insurance;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured.Interfaces
{
    public interface IEnrollmentOnOnboardingRepository : IBaseRepository<EnrollmentOnOnboarding>
    {
        Task<EnrollmentOnOnboarding> GetLastEnrollmentByInsuranceProfileId(int insuranceProfileId);
    }
}
