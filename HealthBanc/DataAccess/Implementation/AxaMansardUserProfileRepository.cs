using HealthBanc.Data;
using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.DataAccess.Implementation
{
    public class AxaMansardUserProfileRepository : BaseRepository<AxaMansardUserProfile>, IAxaMansardUserProfileRepository
    {
        public AxaMansardUserProfileRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
