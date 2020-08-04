using HealthBanc.DataAccess.Implementation;
using HealthBanc.Domain.Models;
using HealthBanc.DTO.DashboardAnalyticsDTOs;
using HealthBanc.Request;
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
        Task<List<ApplicationUser>> GetAllUsers(PaginationQuery paginationQuery = null);
        Task<ApplicationUser> GetByEmailAsync(string email);
        Task<DashboardDTO> GetServiceBreakdown();
        Task<DashboardDTO> GetSignUpAnalytics();
        Task<DashboardDTO> GetUsersStatus();
    }
}
