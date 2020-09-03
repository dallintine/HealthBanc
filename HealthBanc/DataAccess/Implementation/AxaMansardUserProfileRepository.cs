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
    public class AxaMansardUserProfileRepository : BaseRepository<AxaMansardUserProfile>, IAxaMansardUserProfileRepository
    {
        public AxaMansardUserProfileRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<AxaMansardUserProfile> GetByAdminIdAsync(int id)
        {
            return await _context.AxaMansardUserProfile.FirstOrDefaultAsync(x => x.SuperAdminId == id);
        }

        public async Task<AxaMansardUserProfile> GetByIdAsync(int id)
        {
            return await _context.AxaMansardUserProfile.FirstOrDefaultAsync(x => x.Id== id);
        }
    }
}
