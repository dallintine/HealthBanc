using DataAccess.General.Implementation;
using DataAccess.HealthInsured_AxaMansard.Interfaces;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured_AxaMansard.Implementation
{
    public class InsuranceCompletionProfileRepository : BaseRepository<InsuranceCompletionProfile>, IInsuranceCompletionProfileRepository
    {
        public InsuranceCompletionProfileRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<InsuranceCompletionProfile> GetCompletionStateByUserId(int userId)
        {
            return await _context.InsuranceCompletionProfiles.FirstOrDefaultAsync(x => x.UserId == userId);
        }
    }
}
