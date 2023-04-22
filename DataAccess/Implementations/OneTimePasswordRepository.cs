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
    public class OneTimePasswordRepository : BaseRepository<OneTimePassword>, IOneTimePasswordRepository
    {
        public OneTimePasswordRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<OneTimePassword> GetUserLastOTP(long userId, string action)
        {
            return await _context.OneTimePasswords.Where(x => x.ApplicationUserId == userId && x.Action.ToLower() == action.ToLower() && x.Status == false)
               .OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync();
        }
    }
}
