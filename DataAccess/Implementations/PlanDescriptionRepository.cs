using DataAccess.Interfaces;
using Domain.Entities;
using Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Implementations
{
    public class PlanDescriptionRepository : BaseRepository<PlanDescription>, IPlanDescriptionRepository
    {
        public PlanDescriptionRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
