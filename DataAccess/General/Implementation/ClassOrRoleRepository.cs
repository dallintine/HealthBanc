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
    public class ClassOrRoleRepository : BaseRepository<ClassOrRole>, IClassOrRoleRepository
    {
        public ClassOrRoleRepository(ApplicationDbContext context) : base(context)
        {
        }
        public async Task<ClassOrRole> GetRole(int Id)
        {
            var role = await _context.ClassOrRoles.FirstOrDefaultAsync(x => x.Id == Id);
            return role;
        }

        public async Task<List<ClassOrRole>> GetAdminRoles()
        {
            var roles = await _context.ClassOrRoles.Where(x => x.Id > 5).ToListAsync();
            return roles;
        }

        public async Task<List<ClassOrRole>> GetAllRole()
        {
            var roles = await _context.ClassOrRoles.ToListAsync();
            return roles;
        }
    }
}
