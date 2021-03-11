using DataAccess.General.Implementation;
using DataAccess.HealthInsured.Interfaces;
using Domain.Models.Axa_Hygeia_Insurance;
using Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccess.HealthInsured.Implementation
{
    public class EnrollmentOnOnboardingRepository : BaseRepository<EnrollmentOnOnboarding>, IEnrollmentOnOnboardingRepository
    {
        public EnrollmentOnOnboardingRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
