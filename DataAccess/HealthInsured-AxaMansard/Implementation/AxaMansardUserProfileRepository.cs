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
    public class AxaMansardUserProfileRepository : BaseRepository<AxaMansardUserProfile>, IAxaMansardUserProfileRepository
    {
        public AxaMansardUserProfileRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<AxaMansardUserProfile> GetByUserIdAsync(int id)
        {
            return await _context.AxaMansardUserProfile.Include(x => x.Cards).FirstOrDefaultAsync(x => x.UserId == id);
        }

        public async Task<AxaMansardUserProfile> GetByEmail(string email)
        {
            return await _context.AxaMansardUserProfile.FirstOrDefaultAsync(x => x.Email == email);
        }

        public async Task<AxaMansardUserProfile> GetByIdAsync(int id)
        {
            return await _context.AxaMansardUserProfile.Include(x => x.Cards).FirstOrDefaultAsync(x => x.Id == id);
        }
    }
}
