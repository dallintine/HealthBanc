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
    public class UserSessionRepository : BaseRepository<UserSession>, IUserSessionRepository
    {
        public UserSessionRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<UserSession> GetByUserId_Device(long userId, string ip)
        {
            return await _context.UserSessions.Where(x => x.UserId == userId && x.DeviceIp == ip).OrderByDescending(x => x.Id).FirstOrDefaultAsync();
        }
    }
}
