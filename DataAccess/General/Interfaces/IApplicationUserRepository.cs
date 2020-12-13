using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.General.Interfaces
{
    public interface IApplicationUserRepository : IBaseRepository<ApplicationUser>
    {
        Task<ApplicationUser> FindByEmailAsync(string email);
        Task<ApplicationUser> FindByIdAsync(int id);
        Task<ApplicationUser> FindByUniqueUsername(string username);
        Task<PagedResponse<ApplicationUser>> GetAllUsers(PaginationQuery paginationQuery = null);
        Task<ApplicationUser> GetByEmailAsync(string email);
        IQueryable<ApplicationUser> GetQueraybaleUser();
    }
}
