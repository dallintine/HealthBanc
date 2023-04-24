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

        public async Task<List<Plan>> FetchPlans(long productId)
        {
            return await _context.Plans.Where(x => x.ProductId == productId && !x.IsDeleted).ToListAsync();
        }
    }
}
