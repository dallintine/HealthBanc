using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Domain.Models;
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

        //WORKING1
        /// <summary>
        /// Get All Dashboard Analytics
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<DashboardDTO>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize]
        [HttpGet("[action]")]
        public async Task<IActionResult> DashboardAnalytics()
        {
            try
            {
                var users = await _userRepository.GetUsersStatus();
                var signUpAnalytics = await _userRepository.GetSignUpAnalytics(null);
                var serviceBreakdown = await _userRepository.GetServiceBreakdown(null);
                var dashbordDTO = new DashboardDTO()
                {
                    ActiveUsers = users.ActiveUsers,
                    RegisteredUsers = users.RegisteredUsers,
                    InactiveUsers = users.InactiveUsers,
                    ServiceBreakdowns = serviceBreakdown.ServiceBreakdowns,
                    SignUpMonths = signUpAnalytics.SignUpMonths
                };

                return Ok(new ResponseMessage<DashboardDTO> { Data = dashbordDTO, Status = true, Message = "Dashboard DTO was fetched successfully" });
            }
            catch(Exception ex)
            {
                _logger.LogCritical("An error occurred while trying to get dashboard analytics", ex);
                return BadRequest(new ResponseMessage { Message = "An error occurred while trying to get dashboard analytics"});
            }

        }

        //WORKING1
        /// <summary>
        /// Get Users Status
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<DashboardDTO>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize]
        [HttpGet("[action]")]
        public async Task<IActionResult> GetUsersStatus()
        {
            try
            {
                var users = await _userRepository.GetUsersStatus();
                var dashbordDTO = new DashboardDTO()
                {
                    ActiveUsers = users.ActiveUsers,
                    RegisteredUsers = users.RegisteredUsers,
                    InactiveUsers = users.InactiveUsers,
                };
                return Ok(new ResponseMessage { Data = dashbordDTO, Status = true, Message = "Dashboard DTO was fetched successfully" });
            }
            catch(Exception ex)
            {
                _logger.LogCritical("An error occurred while trying to get user status", ex);
                return BadRequest(new ResponseMessage { Message = "An error occurred while trying to get user status" });
            }
        }

        //WORKING1
        /// <summary>
        /// Get Service Breakdown
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<DashboardDTO>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        //[Authorize]
        [HttpGet("[action]")]
        public async Task<IActionResult> GetServiceBreakdown([FromQuery] int? timeId)
        {
            try
            {
                if ((timeId > 0 && timeId < 3) || timeId is null)
                {
                    var serviceBreakdown = await _userRepository.GetServiceBreakdown(timeId);
                    var dashbordDTO = new DashboardDTO()
                    {
                        ServiceBreakdowns = serviceBreakdown.ServiceBreakdowns
                    };
                    return Ok(new ResponseMessage { Data = dashbordDTO, Status = true, Message = "Dashboard DTO was fetched successfully" });
                }
                return BadRequest(new ResponseMessage { Message = "timeId is invalid" });
            }
            catch (Exception ex)
            {
                _logger.LogCritical("An error occurred while trying to get GetServiceBreakdown", ex);
                return BadRequest(new ResponseMessage { Message = "An error occurred while trying to process service breakdown" });
            }

        }

        //WORKING1
        /// <summary>
        /// Get SignUp Analytics
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<DashboardDTO>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize]
        [HttpGet("[action]")]
        public async Task<IActionResult> GetSignUpAnalytics([FromQuery] int? timeId)
        {
            try
            {
                if (timeId == 1 || timeId is null)
                {
                    var signUpAnalytics = await _userRepository.GetSignUpAnalytics(timeId);
                    var dashbordDTO = new DashboardDTO()
                    {
                        SignUpMonths = signUpAnalytics.SignUpMonths
                    };
                    return Ok(new ResponseMessage { Data = dashbordDTO, Status = true, Message = "Dashboard DTO was fetched successfully" });
                }
                return BadRequest(new ResponseMessage { Message = "timeId is invalid" });
            }
            catch (Exception ex)
            {
                _logger.LogCritical("An error occurred while trying to get GetServiceBreakdown", ex);
                return BadRequest(new ResponseMessage { Message = "An error occurred while trying to process service breakdown" });
            }
        }

        //WORKING1
        /// <summary>
        /// Get ALl Service
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<List<Service>>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
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
