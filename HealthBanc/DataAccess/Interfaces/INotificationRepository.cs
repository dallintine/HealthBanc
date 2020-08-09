using HealthBanc.DataAccess.Implementation;
using HealthBanc.Domain.Models;
using HealthBanc.Request;
using HealthBanc.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.DataAccess.Interfaces
{
    public interface INotificationRepository : IBaseRepository<Notification>
    {
        Task<PagedResponse<Notification>> GetAllNotifications(PaginationQuery paginationQuery);
        Task<Notification> GetNotificationById(int Id);
    }
}
