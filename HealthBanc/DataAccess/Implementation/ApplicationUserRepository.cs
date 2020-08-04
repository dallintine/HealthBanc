using AutoMapper;
using HealthBanc.Data;
using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Domain.Models;
using HealthBanc.DTO.DashboardAnalyticsDTOs;
using HealthBanc.Request;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task<List<ApplicationUser>> GetAllUsers(PaginationQuery paginationQuery)
        {
            var queryable = _context.Users.AsQueryable();

            if(paginationQuery is null)
            {
                return await _context.Users.ToListAsync();
            }

            if (!string.IsNullOrEmpty(paginationQuery.SearchText))
            {
                queryable = queryable.Where(x => x.FirstName.Contains(paginationQuery.SearchText) || x.LastName.Contains(paginationQuery.SearchText) ||
                x.Email.Contains(paginationQuery.SearchText) || x.PhoneNumber.Contains(paginationQuery.SearchText));
            }

            queryable = paginationQuery.SortBy == 1 ? queryable.OrderBy(s => s.FirstName) : paginationQuery.SortBy == 2 ? queryable.OrderBy(s => s.LastName) :
                paginationQuery.SortBy == 3 ? queryable.OrderBy(s => s.Email) : queryable.OrderByDescending(s => s.DateOfRegistration);

            var skip = (paginationQuery.PageNumber - 1) * paginationQuery.PageSize;

            if (paginationQuery.Filter is null)
            {
                return await queryable.Skip(skip).Take(paginationQuery.PageSize).ToListAsync();
            }
            else
            {
                return await queryable.Where(x => x.ServiceUsed.Contains(paginationQuery.Filter.ToString())).Skip(skip).Take(paginationQuery.PageSize).ToListAsync();
            }
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

        public async Task<DashboardDTO> GetUsersStatus()
        {
            var registeredUsers = await _context.Users.CountAsync();
            var activeUsers = await _context.Users.Where(x => x.LastLoginDate.AddDays(30) >= DateTime.Now).CountAsync();
            var inactiveUsers = registeredUsers - activeUsers;
            var dashboardDTO = new DashboardDTO
            {
                RegisteredUsers = registeredUsers,
                ActiveUsers = activeUsers,
                InactiveUsers = inactiveUsers
            };
            return dashboardDTO;
        }

        public async Task<DashboardDTO> GetServiceBreakdown()
        {
            var services = await _context.Services.ToListAsync();
            var dashboardServiceList = _mapper.Map<List<Service>, List<ServiceBreakdown>>(services);
            foreach(var item in dashboardServiceList)
            {
                 item.Count = await _context.Users.Where(x => x.ServiceUsed.Contains(item.Id.ToString())).CountAsync();
            }
            var dashboardDTO = new DashboardDTO();
            dashboardDTO.ServiceBreakdowns = dashboardServiceList;
            return dashboardDTO;
        }

        public async Task<DashboardDTO> GetSignUpAnalytics()
        {
            string[] months = new string[]{ "Janaury", "February", "March", "April","May","June","July","August","September","October","November","December" };
            var count = 1;
            var dashboardDTO = new DashboardDTO();

            dashboardDTO.SignUpMonths = new List<SignUpMonth>();

            foreach (var item in months)
            {
                var userRegisteredInParticularMonth = await _context.Users.Where(x => x.DateOfRegistration.Year == DateTime.Now.Year &&
                x.DateOfRegistration.Month == count).CountAsync();
                var signUpMonth = new SignUpMonth(item, userRegisteredInParticularMonth);
                dashboardDTO.SignUpMonths.Add(signUpMonth);
                count++;
            }
            return dashboardDTO;
        }
    }
}
