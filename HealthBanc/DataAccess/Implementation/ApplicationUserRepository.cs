using AutoMapper;
using HealthBanc.Data;
using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Domain.Models;
using HealthBanc.DTO.DashboardAnalyticsDTOs;
using HealthBanc.Request;
using HealthBanc.Response;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace HealthBanc.DataAccess.Implementation
{
    public class ApplicationUserRepository : BaseRepository<ApplicationUser>, IApplicationUserRepository
    {
        private readonly IMapper _mapper;

        public ApplicationUserRepository(ApplicationDbContext context,IMapper mapper) : base(context)
        {
            _mapper = mapper;
        }

        public async Task<PagedResponse<ApplicationUser>> GetAllUsers(PaginationQuery paginationQuery)
        {
            var paginatedResponse = new PagedResponse<ApplicationUser>();
            var queryable = _context.Users.Where(x => x.UniqueUsername == null).AsQueryable();

            if(paginationQuery is null)
            {
                paginatedResponse.Data = await queryable.ToListAsync();
                return paginatedResponse;
            }
            //If Status is null returns all registered users
            //if status is 1 returns active users
            //if status is 2 returns deactivated users
            if (paginationQuery.Status != null)
            {
                if (paginationQuery.Status == 1) queryable = queryable.Where(x => x.LastLoginDate.AddDays(30) >= DateTime.Now).AsQueryable();
                if (paginationQuery.Status == 2) queryable = queryable.Where(x => x.LastLoginDate.AddDays(30) <= DateTime.Now).AsQueryable();
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
                var newQueryable = queryable.Where(x => x.ServiceUsed.Contains(paginationQuery.Filter.ToString())).Skip(skip).Take(paginationQuery.PageSize);
                paginatedResponse.Data = await newQueryable.ToListAsync();
                var recordCount = await queryable.CountAsync();
                paginatedResponse.RecordCount = recordCount;
                paginatedResponse.PageCount = Convert.ToInt32(Math.Ceiling((double)recordCount / (double)paginationQuery.PageSize));
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
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == email);
            return user;
        }

        public async Task<DashboardDTO> GetUsersStatus()
        {
            var users = _context.Users.Where(x => x.UniqueUsername == null).AsQueryable();
            var registeredUsers = await users.CountAsync();
            var activeUsers = await users.Where(x => x.LastLoginDate.AddDays(30) >= DateTime.Now).CountAsync();
            var inactiveUsers = registeredUsers - activeUsers;
            var dashboardDTO = new DashboardDTO
            {
                RegisteredUsers = registeredUsers,
                ActiveUsers = activeUsers,
                InactiveUsers = inactiveUsers
            };
            return dashboardDTO;
        }

        public async Task<DashboardDTO> GetServiceBreakdown(int? Id)
        {
            var services = await _context.Services.ToListAsync();
            var users = _context.Users.Where(x => x.UniqueUsername == null).AsQueryable();
            var dashboardServiceList = _mapper.Map<List<Service>, List<ServiceBreakdown>>(services);
            if(Id is null)
            {
                foreach (var item in dashboardServiceList)
                {
                    item.Count = await users.Where(x => x.ServiceUsed.Contains(item.Name.ToString())).CountAsync();
                }
            }
            else if(Id == 1)
            {
                foreach (var item in dashboardServiceList)
                {
                    item.Count = await users.Where(x => x.ServiceUsed.Contains(item.Name.ToString()) && x.DateOfRegistration.Year == DateTime.Now.Year).CountAsync();
                }
            }
            else if(Id == 2)
            {
                foreach (var item in dashboardServiceList)
                {
                    item.Count = await users.Where(x => x.ServiceUsed.Contains(item.Name.ToString()) && x.DateOfRegistration.Month == DateTime.Now.Month).CountAsync();
                }
            }           
            var dashboardDTO = new DashboardDTO();
            dashboardDTO.ServiceBreakdowns = dashboardServiceList;
            return dashboardDTO;
        }

        public async Task<DashboardDTO> GetSignUpAnalytics(int? Id)
        {
            string[] months = new string[]{ "Janaury", "February", "March", "April","May","June","July","August","September","October","November","December" };
            var count = 1;
            var users = _context.Users.Where(x => x.UniqueUsername == null).AsQueryable();
            var dashboardDTO = new DashboardDTO();

            dashboardDTO.SignUpMonths = new List<SignUpMonth>();

            if(Id is null)
            {
                foreach (var item in months)
                {
                    var userRegisteredInParticularMonth = await users.Where(x => x.DateOfRegistration.Month == count).CountAsync();
                    var signUpMonth = new SignUpMonth(item, userRegisteredInParticularMonth);
                    dashboardDTO.SignUpMonths.Add(signUpMonth);
                    count++;
                }
            }
            else if(Id == 1)
            {
                foreach (var item in months)
                {
                    var userRegisteredInParticularMonth = await users.Where(x => x.DateOfRegistration.Year == DateTime.Now.Year &&
                    x.DateOfRegistration.Month == count).CountAsync();
                    var signUpMonth = new SignUpMonth(item, userRegisteredInParticularMonth);
                    dashboardDTO.SignUpMonths.Add(signUpMonth);
                    count++;
                }
            }
           
            return dashboardDTO;
        }
    }
}
