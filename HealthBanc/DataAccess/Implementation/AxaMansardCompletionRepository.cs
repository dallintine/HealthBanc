using HealthBanc.Data;
using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.DataAccess.Implementation
{
    public class AxaMansardCompletionRepository : BaseRepository<AxaMansardCompletionProfile>, IAxaMansardCompletionRepository
    {
        public AxaMansardCompletionRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<AxaMansardCompletionProfile> GetCompletionStateBySuperAdminId(int id)
        {
            return await _context.AxaMansardCompletionProfiles.FirstOrDefaultAsync(x => x.SuperAdminId == id);
        }
    }
}
