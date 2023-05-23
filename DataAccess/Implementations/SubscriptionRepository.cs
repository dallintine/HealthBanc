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
    public class SubscriptionRepository : BaseRepository<Subscription>, ISubscriptionRepository
    {
        public SubscriptionRepository(ApplicationDbContext context) : base(context)
        {
        }

        public IQueryable<Subscription> QuerySubscriptions_Plans()
        {
            return _context.Subscriptions.Include(x => x.ApplicationUser).Include(x => x.Plan).Include(x => x.Plan.Vendor).AsQueryable();
        }
    }
}
