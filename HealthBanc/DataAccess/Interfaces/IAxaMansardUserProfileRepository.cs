using HealthBanc.DataAccess.Implementation;
using HealthBanc.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.DataAccess.Interfaces
{
    public interface IAxaMansardUserProfileRepository : IBaseRepository<AxaMansardUserProfile>
    {
        Task<AxaMansardUserProfile> GetByAdminIdAsync(int id);
        Task<AxaMansardUserProfile> GetByIdAsync(int id);
    }
}
