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
    public class AxaMansardCompletionRepository : BaseRepository<AxaMansardCompletionProfile>, IAxaMansardCompletionRepository
    {
        public AxaMansardCompletionRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<AxaMansardCompletionProfile> GetCompletionStateByUserId(int userId)
        {
            return await _context.AxaMansardCompletionProfiles.FirstOrDefaultAsync(x => x.UserId == userId);
        }
    }
}
