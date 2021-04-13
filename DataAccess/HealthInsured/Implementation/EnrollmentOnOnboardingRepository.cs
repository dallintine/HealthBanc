using DataAccess.General.Implementation;
using DataAccess.HealthInsured.Interfaces;
using Domain.Models.Axa_Hygeia_Insurance;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured.Implementation
{
    public class EnrollmentOnOnboardingRepository : BaseRepository<EnrollmentOnOnboarding>, IEnrollmentOnOnboardingRepository
    {
        public EnrollmentOnOnboardingRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<EnrollmentOnOnboarding> GetLastEnrollmentByInsuranceProfileId(int insuranceProfileId)
        {
            return await _context.EnrollmentOnOnboardings.LastOrDefaultAsync(x => x.InsuranceUserProfileId == insuranceProfileId);
        }
    }
}
