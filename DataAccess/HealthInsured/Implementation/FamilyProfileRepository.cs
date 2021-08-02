using DataAccess.General.Implementation;
using DataAccess.HealthInsured.Interfaces;
using Domain.Models.Axa_Hygeia_Insurance;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured.Implementation
{
    public class FamilyProfileRepository : BaseRepository<FamilyProfile>, IFamilyProfileRepository
    {
        public FamilyProfileRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<FamilyProfile> GetByUserId(int userId)
        {
            return await _context.FamilyProfiles.Include(x => x.Cards).FirstOrDefaultAsync(x => x.UserId == userId);
        }

        public async Task<IQueryable<InsuranceUserProfile>> QueryFamilyMembers(int userId)
        {
            var familyMembers = await _context.FamilyProfiles.Include(x => x.InsuranceUserProfiles).FirstOrDefaultAsync(x => x.UserId == userId);
            return familyMembers.InsuranceUserProfiles.AsQueryable();
        }

        public async Task<FamilyProfile> GetByEmail(string email)
        {
            return await _context.FamilyProfiles.FirstOrDefaultAsync(x => x.Email == email);
        }
    }
}
