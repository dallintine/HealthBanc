using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Interfaces
{
    public interface ISubscriptionRepository : IBaseRepository<Subscription>
    {
        IQueryable<Subscription> QuerySubscriptions_Plans();
    }
}
