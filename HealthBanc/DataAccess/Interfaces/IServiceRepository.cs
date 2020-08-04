using HealthBanc.DataAccess.Implementation;
using HealthBanc.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.DataAccess.Interfaces
{
    public interface IServiceRepository : IBaseRepository<Service>
    {
        Task<Service> GetServiceById(int id);
        Task<List<Service>> GetServicesAsync();
    }
}
