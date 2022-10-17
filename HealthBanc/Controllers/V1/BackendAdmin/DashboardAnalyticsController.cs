using Application.DTO;
using Application.DTO.DashboardAnalyticsDTOs;
using Application.Services.Admin;
using DataAccess;
using DataAccess.General.Interfaces;
using DataAccess.HealthInsured.Interfaces;
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
        private readonly IRepositoryWrapper _repoWrapper;

        public DashboardAnalyticsController(Dashboard_Analytics dashboardAnalytics,IRepositoryWrapper repoWrapper)
        {
            _dashboardAnalytics = dashboardAnalytics;
            _repoWrapper = repoWrapper;
        }

        /// <summary>
        /// Get Basic Dashboard Analytics for  Healthbanc Users
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
        /// Get Application Users Status
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
        /// Get breakdown of service used by application users
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
            var services = await _repoWrapper.Service.GetServicesAsync();
            return Ok(new ResponseMessage { Data = services, Status = true, Message = "Service was fetched successfully" });            
        }

        /// <summary>
        /// Get Dashboard Analytics for  HealthInsured Service
        /// </summary>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<HealthInsuredDashboardDTO>))]
        [HttpGet("[action]")]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        public async Task<IActionResult> HealthInsuredDashBoardAnalytics([FromQuery] int? insuranceServiceId)
        {
            var healthInsuredDashboard = await _dashboardAnalytics.GetHealthInsuredDashBoardAnalytics(insuranceServiceId);

            return Ok(healthInsuredDashboard);
        }

        /// <summary>
        /// Get HealthInsured User acquisition analytical data
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<HealthInsuredDashboardDTO>))]
        [HttpGet("[action]")]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        public async Task<IActionResult> HealthInsuredUserAcquisition(int? id)
        {
            var healthInsuredAcquisistions = await _dashboardAnalytics.HealthInsuredUserAcquisition(id);
            return Ok(healthInsuredAcquisistions);
        }

        /// <summary>
        /// Get HealthInsured Subscriber acquisition analytical data
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<HealthInsuredDashboardDTO>))]
        [HttpGet("[action]")]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        public async Task<IActionResult> HealthInsuredSubscriberAcquisition(int? id)
        {
            var subscriberAcqusition = await _dashboardAnalytics.HealthInsuredSubscriberAcquisition(id);
            return Ok(subscriberAcqusition);
        }
    }
}
