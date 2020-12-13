using DataAccess.General.Interfaces;
using DataAccess.HealthInsured_AxaMansard.Interfaces;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.General.Implementation
{
    public class BackendAdminRepository : BaseRepository<BackendAdminUser>, IBackendAdminRepository
    {
        public BackendAdminRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<BackendAdminUser>> GetBackendAdmins()
        {
            var users = await _context.BackendAdminUsers.Include(x => x.ClassOrRole).ToListAsync();
            return users;
        }

        public async Task<BackendAdminUser> GetAdminByEmail(string email)
        {
            var admin = await _context.BackendAdminUsers.FirstOrDefaultAsync(x => x.Email == email);
            return admin;
        }
    }
}
