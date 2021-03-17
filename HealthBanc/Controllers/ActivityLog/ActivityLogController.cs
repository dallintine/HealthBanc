using Application.DTO;
using Application.Services.AuditAndReport;
using DataAccess;
using Domain.Models.Axa.Hygeia_Insurance;
using Domain.Models.ExceptionLog;
using Domain.Models.ReportAndLogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HealthBanc.Controllers.Activity_ErrorLog
{
    [Route("v1/api/[controller]")]
    [ApiController]
    public class ActivityLogController : ControllerBase
    {
        public Activity_ErrorLogService _activity_ErrorLogService { get; }

        public ActivityLogController(Activity_ErrorLogService activity_ErrorLogService)
        {
            _activity_ErrorLogService = activity_ErrorLogService;
        }

        /// <summary>
        /// Returns Paginated healthinsured activity log based on email parameter. Applies for both corporate and individual users
        /// </summary>
        /// <param name="paginationQuery"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<PagedResponse<ActivityLog>>))]
        public async Task<IActionResult> GetHealthInsuredPaginatedActivityLogByEmail([FromQuery]PaginationQuery paginationQuery, string email)
        {
            var response = await _activity_ErrorLogService.GetHealthInsuredPaginatedActivityLogByEmail(paginationQuery, email);
            if (response.Status)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        /// <summary>
        /// Returns paginated health insured activity log for logged in users. Applies for both corporate and individual users
        /// </summary>
        /// <param name="paginationQuery"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<PagedResponse<ActivityLog>>))]
        public async Task<IActionResult> GetLoggedInUserHealthInsuredPaginatedActivityLog([FromQuery]PaginationQuery paginationQuery)
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int id = int.Parse(userId);

            var response = await _activity_ErrorLogService.GetLoggedInUserHealthInsuredPaginatedActivityLog(paginationQuery, id);
            if (response.Status)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }
    }
}
