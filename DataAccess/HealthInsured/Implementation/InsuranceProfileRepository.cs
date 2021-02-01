using DataAccess.General.Implementation;
using DataAccess.HealthInsured.Interfaces;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured.Implementation
{
    public class InsuranceProfileRepository : BaseRepository<InsuranceUserProfile>, IInsuranceProfileRepository
    {
        public InsuranceProfileRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<InsuranceUserProfile> GetByUserIdAsync(int id)
        {
            return await _context.InsuranceUserProfiles.Include(x => x.Cards).FirstOrDefaultAsync(x => x.UserId == id);
        }

        public async Task<InsuranceUserProfile> GetByEmail(string email)
        {
            return await _context.InsuranceUserProfiles.Include(x => x.Cards).FirstOrDefaultAsync(x => x.Email == email);
        }

        public async Task<InsuranceUserProfile> GetByIdAsync(int id)
        {
            return await _context.InsuranceUserProfiles.Include(x => x.Cards).FirstOrDefaultAsync(x => x.Id == id);
        }
    }
}
