using Application.DTO;
using Application.DTO.DashboardAnalyticsDTOs;
using Application.Interfaces;
using Application.Services.Admin;
using Application.ViewModels;
using DataAccess;
using DataAccess.General.Interfaces;
using DataAccess.HealthInsured.Interfaces;
using Domain.Models;
using HealthBanc.DTO.DashboardAnalyticsDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Controllers.V2.BackendAdmin
{

    [Route("v{version:apiVersion}/api/[controller]")]
    [ApiController]
    [ApiVersion("2.0")]
    public class DashboardAnalyticsController : ControllerBase
    {
        private readonly Dashboard_Analytics _dashboardAnalytics;
        private readonly IRepositoryWrapper _repoWrapper;
        private readonly IEncryptAndDecrypt _encryptDecrypt;

        public DashboardAnalyticsController(Dashboard_Analytics dashboardAnalytics,IRepositoryWrapper repoWrapper, IEncryptAndDecrypt encryptDecrypt)
        {
            _dashboardAnalytics = dashboardAnalytics;
            _repoWrapper = repoWrapper;
            _encryptDecrypt = encryptDecrypt;
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
        [HttpPost("[action]")]
        public async Task<IActionResult> GetServiceBreakdown(EncryptedModel encryptedModel)
        {
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 });
            var model = JsonConvert.DeserializeObject<IdViewModel>(decryptedString.Item2);

            if (model.Id is null || (model.Id > 0 && model.Id < 3))
            {
                var serviceBreakdown = await _dashboardAnalytics.GetServiceBreakdown(model.Id);
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
        [HttpPost("[action]")]
        public async Task<IActionResult> GetSignUpAnalytics(EncryptedModel encryptedModel)
        {
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 });
            var model = JsonConvert.DeserializeObject<IdViewModel>(decryptedString.Item2);

            if (model.Id is null || model.Id == 1)
            {
                var signUpAnalytics = await _dashboardAnalytics.GetSignUpAnalytics(model.Id);
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
        [HttpPost("[action]")]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        public async Task<IActionResult> HealthInsuredDashBoardAnalytics(EncryptedModel encryptedModel)
        {
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 });
            var model = JsonConvert.DeserializeObject<IdViewModel>(decryptedString.Item2);

            var healthInsuredDashboard = await _dashboardAnalytics.GetHealthInsuredDashBoardAnalytics(model.Id);

            return Ok(healthInsuredDashboard);
        }

        /// <summary>
        /// Get HealthInsured User acquisition analytical data
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<HealthInsuredDashboardDTO>))]
        [HttpPost("[action]")]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        public async Task<IActionResult> HealthInsuredUserAcquisition(EncryptedModel encryptedModel)
        {
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 });
            var model = JsonConvert.DeserializeObject<IdViewModel>(decryptedString.Item2);

            var healthInsuredAcquisistions = await _dashboardAnalytics.HealthInsuredUserAcquisition(model.Id);
            return Ok(healthInsuredAcquisistions);
        }

        /// <summary>
        /// Get HealthInsured Subscriber acquisition analytical data
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<HealthInsuredDashboardDTO>))]
        [HttpPost("[action]")]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        public async Task<IActionResult> HealthInsuredSubscriberAcquisition(EncryptedModel encryptedModel)
        {
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 });
            var model = JsonConvert.DeserializeObject<IdViewModel>(decryptedString.Item2);

            var subscriberAcqusition = await _dashboardAnalytics.HealthInsuredSubscriberAcquisition(model.Id);
            return Ok(subscriberAcqusition);
        }
    }
}
