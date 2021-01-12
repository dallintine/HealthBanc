using DataAccess.General.Interfaces;
using DataAccess.HealthInsured_AxaMansard.Interfaces;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DataAccess.General.Implementation
{
    public class ApplicationUserRepository : BaseRepository<ApplicationUser>, IApplicationUserRepository
    {
        public ApplicationUserRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<PagedResponse<ApplicationUser>> GetAllUsers(PaginationQuery paginationQuery)
        {
            var paginatedResponse = new PagedResponse<ApplicationUser>();
            var queryable = _context.Users.Where(x => x.UniqueUsername == null).AsQueryable();

            if(paginationQuery is null)
            {
                paginatedResponse.Data = await queryable.ToListAsync();
                var recordCount = await queryable.CountAsync();
                paginatedResponse.RecordCount = recordCount;
                paginatedResponse.PageCount = Convert.ToInt32(Math.Ceiling((double)recordCount / (double)paginationQuery.PageSize));
                return paginatedResponse;
            }
            //If Status is null returns all registered users
            //if status is 1 returns active users
            //if status is 2 returns inactive users
            //if status is 3 returns deactivated or locked out users
            if (paginationQuery.Status != null)
            {
                if (paginationQuery.Status == 1) queryable = queryable.Where(x => x.LastLoginDate.AddDays(30) >= DateTime.Now).AsQueryable();
                if (paginationQuery.Status == 2) queryable = queryable.Where(x => x.LastLoginDate.AddDays(30) <= DateTime.Now).AsQueryable();
                if (paginationQuery.Status == 3) queryable = queryable.Where(x => x.LockoutEnd != null).AsQueryable();
            }

            if (!string.IsNullOrEmpty(paginationQuery.SearchText))
            {
                queryable = queryable.Where(x => x.FirstName.Contains(paginationQuery.SearchText) || x.LastName.Contains(paginationQuery.SearchText) ||
                x.Email.Contains(paginationQuery.SearchText) || x.PhoneNumber.Contains(paginationQuery.SearchText));
            }

            //Sort the users
            queryable = paginationQuery.SortBy == 1 ? queryable.OrderBy(s => s.FirstName) : paginationQuery.SortBy == 2 ? queryable.OrderBy(s => s.LastName) :
                paginationQuery.SortBy == 3 ? queryable.OrderBy(s => s.Email) : queryable.OrderByDescending(s => s.DateOfRegistration);

            var skip = (paginationQuery.PageNumber - 1) * paginationQuery.PageSize;           

            if (paginationQuery.Filter is null)
            {
                var newQueryable = queryable.Skip(skip).Take(paginationQuery.PageSize).AsQueryable();
                paginatedResponse.Data =  await newQueryable.ToListAsync();
                var recordCount = await queryable.CountAsync();
                paginatedResponse.RecordCount = recordCount;
                paginatedResponse.PageCount = Convert.ToInt32(Math.Ceiling((double)recordCount / (double)paginationQuery.PageSize));
                return paginatedResponse;
            }
            else 
            {
                //filter users by service used via their the Service ID.
                if(paginationQuery.Filter == 1)
                {
                    var newQueryable = queryable.Where(x => x.ServiceUsed.Contains("HealthMall")).Skip(skip).Take(paginationQuery.PageSize);
                    paginatedResponse.Data = await newQueryable.ToListAsync();
                    var recordCount = await queryable.CountAsync();
                    paginatedResponse.RecordCount = recordCount;
                    paginatedResponse.PageCount = Convert.ToInt32(Math.Ceiling((double)recordCount / (double)paginationQuery.PageSize));
                    return paginatedResponse;
                }
                else if (paginationQuery.Filter == 2)
                {
                    var newQueryable = queryable.Where(x => x.ServiceUsed.Contains("HealthInsured")).Skip(skip).Take(paginationQuery.PageSize);
                    paginatedResponse.Data = await newQueryable.ToListAsync();
                    var recordCount = await queryable.CountAsync();
                    paginatedResponse.RecordCount = recordCount;
                    paginatedResponse.PageCount = Convert.ToInt32(Math.Ceiling((double)recordCount / (double)paginationQuery.PageSize));
                    return paginatedResponse;
                }
                return paginatedResponse;
            } 
        }

        public async Task<ApplicationUser> FindByEmailAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email && x.UniqueUsername == null);
            return user;
        }

        public async Task<ApplicationUser>  FindByIdAsync(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id);
            return user;
        }

        public async Task<ApplicationUser> FindByUniqueUsername(string username)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.UniqueUsername == username);
            return user;
        }       

        public async Task<ApplicationUser> GetByEmailAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
            return user;
        }

        public IQueryable<ApplicationUser> GetQueraybaleUser()
        {
            return _context.Users.Where(x => x.UniqueUsername == null).AsQueryable();
        }
    }
}
