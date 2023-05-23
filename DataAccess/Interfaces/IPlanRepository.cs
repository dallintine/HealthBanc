using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Interfaces
{
    public interface IPlanRepository : IBaseRepository<Plan>
    {
        Task<List<Plan>> FetchPlans(long serviceId);
        IQueryable<Plan> QueryPlans(long serviceId);
    }
}
