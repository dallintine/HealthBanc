using Application.DTO;
using Application.Services.Admin;
using DataAccess.General.Interfaces;
using DataAccess.HealthInsured_AxaMansard.Interfaces;
using Domain.Models;
using HealthBanc.DTO.DashboardAnalyticsDTOs;
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
        private readonly Dashboard_Analytics _dashboardAnalytics;
        private readonly IServiceRepository _serviceRepository;

        public DashboardAnalyticsController(Dashboard_Analytics dashboardAnalytics,IServiceRepository serviceRepository)
        {
            _dashboardAnalytics = dashboardAnalytics;
            _serviceRepository = serviceRepository;
        }

        /// <summary>
        /// Get All Dashboard Analytics
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<DashboardDTO>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        [HttpGet("[action]")]
        public async Task<IActionResult> DashboardAnalytics()
        {
            var allDashboardAnalytics = await _dashboardAnalytics.GetAllDashboardAnalystics();

            return Ok(new ResponseMessage<DashboardDTO> { Data = allDashboardAnalytics, Status = true, Message = "All dashboard analytics was fetched successfully" });           
        }

        
        /// <summary>
        /// Get Users Status
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<DashboardDTO>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        [HttpGet("[action]")]
        public async Task<IActionResult> GetUsersStatus()
        {
            var userStatus = await _dashboardAnalytics.GetUsersStatus();
            return Ok(new ResponseMessage { Data = userStatus, Status = true, Message = "User status was fetched successfully" });
        }

        
        /// <summary>
        /// Get Service Breakdown
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<DashboardDTO>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        [HttpGet("[action]")]
        public async Task<IActionResult> GetServiceBreakdown([FromQuery] int? timeId)
        {
            if ((timeId > 0 && timeId < 3) || timeId is null)
            {
                var serviceBreakdown = await _dashboardAnalytics.GetServiceBreakdown(timeId);
                return Ok(new ResponseMessage { Data = serviceBreakdown, Status = true, Message = "Service breakdown was fetched successfully" });
            }
            return BadRequest(new ResponseMessage { Message = "timeId is invalid" });
        }

        /// <summary>
        /// Get SignUp Analytics
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<DashboardDTO>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        [HttpGet("[action]")]
        public async Task<IActionResult> GetSignUpAnalytics([FromQuery] int? timeId)
        {
            if (timeId == 1 || timeId is null)
            {
                var signUpAnalytics = await _dashboardAnalytics.GetSignUpAnalytics(timeId);
                return Ok(new ResponseMessage { Data = signUpAnalytics, Status = true, Message = "SignUp analytics was fetched successfully" });
            }
            return BadRequest(new ResponseMessage { Message = "timeId is invalid" });
        }

        /// <summary>
        /// Get ALl Service
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<List<Service>>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        public async Task<IActionResult> GetServices()
        {            
            var services = await _serviceRepository.GetServicesAsync();
            return Ok(new ResponseMessage { Data = services, Status = true, Message = "Service was fetched successfully" });            
        }
    }
}
