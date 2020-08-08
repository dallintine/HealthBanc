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
