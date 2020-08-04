using HealthBanc.DataAccess.Implementation;
using HealthBanc.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.DataAccess.Interfaces
{
    public interface IClassOrRoleRepository : IBaseRepository<ClassOrRole>
    {
        Task<List<ClassOrRole>> GetAdminRoles();
        Task<List<ClassOrRole>> GetAllRole();
        Task<ClassOrRole> GetRole(int Id);
    }
}
