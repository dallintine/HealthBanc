using DataAccess.General.Interfaces;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.General.Implementation
{
    public class UserSessionRepository : BaseRepository<UserSession>, IUserSessionRepository
    {
        public UserSessionRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<UserSession> GetById(long id)
        {
            return await _context.UserSessions.SingleOrDefaultAsync(x => x.Id == id);
        }

        public async Task<UserSession> GetById_Device(long id, string ip)
        {
            return await _context.UserSessions.SingleOrDefaultAsync(x => x.Id == id && x.DeviceIp == ip);
        }

        public async Task<UserSession> GetByUserId(int userId)
        {
            return await _context.UserSessions.SingleOrDefaultAsync(x => x.UserId == userId);
        }
    }
}
