using DataAccess.General.Interfaces;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured_AxaMansard.Interfaces
{
    public interface INotificationRepository : IBaseRepository<Notification>
    {
        Task<PagedResponse<Notification>> GetAllNotifications(PaginationQuery paginationQuery);
        Task<Notification> GetNotificationById(int Id);
    }
}
