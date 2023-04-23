using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Interfaces
{
    public interface IUserSessionRepository : IBaseRepository<UserSession>
    {
        Task<UserSession> GetByUserId_Device(long userId, string ip);
    }
}
