using HealthBanc.DataAccess.Interfaces;
using HealthBanc.DTO.DashboardAnalyticsDTOs;
using HealthBanc.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private readonly IServiceRepository _serviceRepository;
        private readonly ILogger<DashboardAnalyticsController> _logger;

        public DashboardAnalyticsController(IApplicationUserRepository userRepository,IServiceRepository serviceRepository,ILogger<DashboardAnalyticsController> logger)
        {
            _userRepository = userRepository;
            _serviceRepository = serviceRepository;
            _logger = logger;
        }

        [Authorize]
        [HttpGet("[action]")]
        public async Task<IActionResult> DashboardAnalytics()
        {
            try
            {
                var users = await _userRepository.GetUsersStatus();
                var signUpAnalytics = await _userRepository.GetSignUpAnalytics();
                var serviceBreakdown = await _userRepository.GetServiceBreakdown();
                var dashbordDTO = new DashboardDTO()
                {
                    ActiveUsers = users.ActiveUsers,
                    RegisteredUsers = users.RegisteredUsers,
                    InactiveUsers = users.InactiveUsers,
                    ServiceBreakdowns = serviceBreakdown.ServiceBreakdowns,
                    SignUpMonths = signUpAnalytics.SignUpMonths
                };

                return Ok(new ResponseMessage { Data = dashbordDTO, Status = true, Message = "Dashboard DTO was fetched successfully" });
            }
            catch(Exception ex)
            {
                _logger.LogCritical("An error occurred while trying to get dashboard analytics", ex);
                return BadRequest(new ResponseMessage { Message = "An error occurred while trying to get dashboard analytics"});
            }

        }

        [Authorize]
        [HttpGet("[action]")]
        public async Task<IActionResult> GetServices()
        {
            try
            {
                var services = await _serviceRepository.GetServicesAsync();
                return Ok(new ResponseMessage { Data = services, Status = true, Message = "Service was fetched successfully" });
            }
            catch(Exception ex)
            {
                _logger.LogCritical("An error occurred while trying to fetch services", ex);
                return BadRequest(new ResponseMessage {Message = "An error occurred while trying to fetch services" });
            }
        }
    }
}
