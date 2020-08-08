using HealthBanc.DataAccess.Implementation;
using HealthBanc.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.DataAccess.Interfaces
{
    public interface IBackendAdminRepository : IBaseRepository<BackendAdminUser>
    {
        Task<BackendAdminUser> GetAdminByEmail(string email);
        Task<List<BackendAdminUser>> GetBackendAdmins();
    }
}
