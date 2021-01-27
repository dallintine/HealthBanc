using DataAccess.General.Implementation;
using DataAccess.HealthInsured_Hygeia.Interface;
using Domain.Models.Hygeia_Insurance;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured_Hygeia.Implementation
{
    public class HygeiaUserProfileRepository : BaseRepository<HygeiaUserProfile>, IHygeiaUserProfileRepository
    {
        public HygeiaUserProfileRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<HygeiaUserProfile> GetByUserIdAsync(int id)
        {
            return await _context.HygeiaUserProfiles.FirstOrDefaultAsync(x => x.UserId == id);
        }
    }
}
