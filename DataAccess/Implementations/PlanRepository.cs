using DataAccess.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Implementations
{
    public class PlanRepository : BaseRepository<Plan>, IPlanRepository
    {
        public PlanRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<Plan>> FetchPlans(long serviceId)
        {
            return await _context.Plans.Where(x => x.ServiceId == serviceId && !x.IsDeleted).ToListAsync();
        }

        public IQueryable<Plan> QueryPlans(long serviceId)
        {
            return _context.Plans.Include(x => x.Vendor).Include(x => x.PlanDescriptions).Where(x => x.ServiceId == serviceId && !x.IsDeleted).AsQueryable();
        }
    }
}
