using DataAccess.General.Implementation;
using DataAccess.HealthInsured_AxaMansard.Interfaces;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured_AxaMansard.Implementation
{
    public class NotificationRepository : BaseRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<PagedResponse<Notification>> GetAllNotifications(PaginationQuery paginationQuery)
        {
            var paginatedResponse = new PagedResponse<Notification>();
            var queryable = _context.Notifications.AsQueryable();

            if (paginationQuery is null)
            {
                paginatedResponse.Data = await queryable.ToListAsync();
                return paginatedResponse;
            }

            //if status is 1 returns sent notification
            //if status is 2 returns draft notification
            //if status is 3 return scheduled notification
            //if status is 4 return trash notification
            if (paginationQuery.Status != null)
            {
                if (paginationQuery.Status == 1) queryable = queryable.Where(x => x.Status == 1).AsQueryable();
                if (paginationQuery.Status == 2) queryable = queryable.Where(x => x.Status == 2).AsQueryable();
                if (paginationQuery.Status == 3) queryable = queryable.Where(x => x.Status == 3).AsQueryable();
                if (paginationQuery.Status == 4) queryable = queryable.Where(x => x.Status == 4).AsQueryable();
            }

            if (!string.IsNullOrEmpty(paginationQuery.SearchText))
            {
                queryable = queryable.Where(x => x.Subject.Contains(paginationQuery.SearchText));
            }

            //Sort the users
            queryable = paginationQuery.SortBy == 1 ? queryable.OrderBy(s => s.Subject) : queryable.OrderByDescending(s => s.ScheduleDate);

            var skip = (paginationQuery.PageNumber - 1) * paginationQuery.PageSize;

            if (paginationQuery.Filter is null)
            {
                var newQueryable = queryable.Skip(skip).Take(paginationQuery.PageSize).AsQueryable();
                paginatedResponse.Data = await newQueryable.Include(x => x.Service).ToListAsync();
                var recordCount = await queryable.CountAsync();
                paginatedResponse.RecordCount = recordCount;
                paginatedResponse.PageCount = Convert.ToInt32(Math.Ceiling((double)recordCount / (double)paginationQuery.PageSize));
                return paginatedResponse;
            }
            else
            {
                //filter users by service used via their the Service ID.
                var newQueryable = queryable.Where(x => x.ServiceId == (paginationQuery.Filter)).Skip(skip).Take(paginationQuery.PageSize);
                paginatedResponse.Data = await newQueryable.Include(x => x.Service).ToListAsync();
                var recordCount = await queryable.CountAsync();
                paginatedResponse.RecordCount = recordCount;
                paginatedResponse.PageCount = Convert.ToInt32(Math.Ceiling((double)recordCount / (double)paginationQuery.PageSize));
                return paginatedResponse;
            }
        }

        public async Task<Notification> GetNotificationById(int Id)
        {
            var notification = await _context.Notifications.FirstOrDefaultAsync(x => x.Id == Id);
            return notification;
        }
    }
}
