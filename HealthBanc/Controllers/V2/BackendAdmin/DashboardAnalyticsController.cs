using Application.DTO;
using Application.DTO.DashboardAnalyticsDTOs;
using Application.Interfaces;
using Application.Services;
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
        private readonly ResponseHelper _responseHelper;

        public DashboardAnalyticsController(Dashboard_Analytics dashboardAnalytics,IRepositoryWrapper repoWrapper, IEncryptAndDecrypt encryptDecrypt,
            ResponseHelper responseHelper)
        {
            _dashboardAnalytics = dashboardAnalytics;
            _repoWrapper = repoWrapper;
            _encryptDecrypt = encryptDecrypt;
            _responseHelper = responseHelper;
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
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(new ResponseMessage<DashboardDTO> { Data = allDashboardAnalytics, Status = true, Message = "All dashboard analytics was fetched successfully" }));
            return Ok(data);           
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
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(new ResponseMessage { Data = userStatus, Status = true, Message = "User status was fetched successfully" }));
            return Ok(data);
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
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var model = JsonConvert.DeserializeObject<IdViewModel>(decryptedString.Item2);

            if (model.Id is null || (model.Id > 0 && model.Id < 3))
            {
                var serviceBreakdown = await _dashboardAnalytics.GetServiceBreakdown(model.Id);
                var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(new ResponseMessage { Data = serviceBreakdown, Status = true, Message = "Service breakdown was fetched successfully" }));
                return Ok(data);
            }
            var data2 = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(new ResponseMessage { Message = "timeId is invalid" }));
            return BadRequest(data2);
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
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var model = JsonConvert.DeserializeObject<IdViewModel>(decryptedString.Item2);

            if (model.Id is null || model.Id == 1)
            {
                var signUpAnalytics = await _dashboardAnalytics.GetSignUpAnalytics(model.Id);
                var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(new ResponseMessage { Data = signUpAnalytics, Status = true, Message = "SignUp analytics was fetched successfully" }));
                return Ok(data);
            }
            var data2 = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(new ResponseMessage { Message = "timeId is invalid" }));
            return BadRequest(data2);
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
            var data2 = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(new ResponseMessage { Data = services, Status = true, Message = "Service was fetched successfully" }));

            return Ok(data2);            
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
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var model = JsonConvert.DeserializeObject<IdViewModel>(decryptedString.Item2);

            var healthInsuredDashboard = await _dashboardAnalytics.GetHealthInsuredDashBoardAnalytics(model.Id);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(healthInsuredDashboard));
            return Ok(data);
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
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var model = JsonConvert.DeserializeObject<IdViewModel>(decryptedString.Item2);

            var healthInsuredAcquisistions = await _dashboardAnalytics.HealthInsuredUserAcquisition(model.Id);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(healthInsuredAcquisistions));
            return Ok(data);
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
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var model = JsonConvert.DeserializeObject<IdViewModel>(decryptedString.Item2);

            var subscriberAcqusition = await _dashboardAnalytics.HealthInsuredSubscriberAcquisition(model.Id);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(subscriberAcqusition));
            return Ok(data);
        }
    }
}
