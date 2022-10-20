using Application.DTO;
using Application.Interfaces;
using Application.Services.AuditAndReport;
using Application.ViewModels.HealthInsured;
using DataAccess;
using DataAccess.DTO.AuditDTO;
using Domain.Models.Axa.Hygeia_Insurance;
using Domain.Models.ExceptionLog;
using Domain.Models.ReportAndLogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HealthBanc.Controllers.V2.Activity_ErrorLog
{
    [Route("v{version:apiVersion}/api/[controller]")]
    [ApiController]
    [ApiVersion("2.0")]
    public class ActivityLogController : ControllerBase
    {
        private readonly IEncryptAndDecrypt _encryptDecrypt;
        private readonly Activity_ErrorLogService _activity_ErrorLogService;

        public ActivityLogController(Activity_ErrorLogService activity_ErrorLogService , IEncryptAndDecrypt encryptDecrypt)
        {
            _activity_ErrorLogService = activity_ErrorLogService;
            _encryptDecrypt = encryptDecrypt;
        }

        /// <summary>
        /// Returns Paginated healthinsured activity log based on email parameter. Applies for both corporate and individual users
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<PagedResponse<ActivityLog>>))]
        public async Task<IActionResult> GetHealthInsuredPaginatedActivityLogByEmail(EncryptedModel encryptedModel)
        {
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 });
            var activityLog = JsonConvert.DeserializeObject<HealthInsuredPaginatedActivityLog>(decryptedString.Item2);
            var response = await _activity_ErrorLogService.GetHealthInsuredPaginatedActivityLogByEmail(activityLog, activityLog.Email);
            if (response.Status)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        /// <summary>
        /// Returns paginated health insured activity log for logged in users. Applies for both corporate and individual users
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<PagedResponse<ActivityLog>>))]
        public async Task<IActionResult> GetLoggedInUserHealthInsuredPaginatedActivityLog(EncryptedModel encryptedModel)
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int id = int.Parse(userId);

            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 });
            var paginationQuery = JsonConvert.DeserializeObject<PaginationQuery>(decryptedString.Item2);

            var response = await _activity_ErrorLogService.GetLoggedInUserHealthInsuredPaginatedActivityLog(paginationQuery, id);
            if (response.Status)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        /// <summary>
        /// Returns paginated backend acivity logs
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<PagedResponse<AdminAuditLogDTO>>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        public async Task<IActionResult> GetAdminActivityLogs(EncryptedModel encryptedModel)
        {
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 });
            var activity = JsonConvert.DeserializeObject<GetAdminActivityLogsViewModel>(decryptedString.Item2);

            var response = await _activity_ErrorLogService.GetAdminActivityLogs(activity, activity.Channel);
            if (response.Status)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }
    }
}
