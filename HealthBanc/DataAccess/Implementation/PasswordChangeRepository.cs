using HealthBanc.Data;
using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Domain.Models.ReportAndLogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.DataAccess.Implementation
{
    public class PasswordChangeRepository : BaseRepository<PasswordChangeHistory>, IPasswordChangeRepository
    {
        public PasswordChangeRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
