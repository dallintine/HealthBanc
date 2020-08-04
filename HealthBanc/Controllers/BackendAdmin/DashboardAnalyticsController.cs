using HealthBanc.DataAccess.Interfaces;
using HealthBanc.DTO.DashboardAnalyticsDTOs;
using HealthBanc.Response;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Controllers.BackendAdmin
{

    [Route("v1/api/[controller]")]
    [ApiController]
    public class DashboardAnalyticsController : ControllerBase
    {
        private readonly IApplicationUserRepository _userRepository;

        public DashboardAnalyticsController(IApplicationUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> DashboardAnalytics()
        {
            var users =await _userRepository.GetUsersStatus();
            var signUpAnalytics =await _userRepository.GetSignUpAnalytics();
            var serviceBreakdown =await _userRepository.GetServiceBreakdown();
            var dashbordDTO = new DashboardDTO()
            {
                ActiveUsers = users.ActiveUsers,
                RegisteredUsers = users.RegisteredUsers,
                InactiveUsers = users.InactiveUsers,
                ServiceBreakdowns = serviceBreakdown.ServiceBreakdowns,
                SignUpMonths = signUpAnalytics.SignUpMonths
            };

            return Ok(new ResponseMessage{ Data = dashbordDTO, Status = true, Message = "Dashboard DTO was fetched successfully" });
        }
    }
}
