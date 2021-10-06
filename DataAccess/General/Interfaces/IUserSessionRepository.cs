using Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.General.Interfaces
{
    public interface IUserSessionRepository : IBaseRepository<UserSession>
    {
        Task<UserSession> GetById(long id);
        Task<UserSession> GetById_Device(long id, string ip);
        Task<UserSession> GetByUserId(int userId);
        Task<UserSession> GetByUserId_Device(int userId, string ip);
    }
}
