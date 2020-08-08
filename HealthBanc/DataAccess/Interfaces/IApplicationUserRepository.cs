using HealthBanc.DataAccess.Implementation;
using HealthBanc.Domain.Models;
using HealthBanc.DTO.DashboardAnalyticsDTOs;
using HealthBanc.Request;
using HealthBanc.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.DataAccess.Interfaces
{
    public interface IApplicationUserRepository : IBaseRepository<ApplicationUser>
    {
        Task<ApplicationUser> FindByIdAsync(int id);
        Task<ApplicationUser> FindByUniqueUsername(string username);
        Task<PagedResponse<ApplicationUser>> GetAllUsers(PaginationQuery paginationQuery = null);
        Task<ApplicationUser> GetByEmailAsync(string email);
        Task<DashboardDTO> GetServiceBreakdown(int? Id);
        Task<DashboardDTO> GetSignUpAnalytics(int? Id);
        Task<DashboardDTO> GetUsersStatus();
    }
}
