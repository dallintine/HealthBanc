using Application.DTO;
using Application.Services.AuditAndReport;
using DataAccess;
using Domain.Models.ExceptionLog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Controllers
{
    [Route("v{version:apiVersion}/api/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class ErrorLogController : ControllerBase
    {
        public Activity_ErrorLogService _activity_ErrorLogService { get; }

        public ErrorLogController(Activity_ErrorLogService activity_ErrorLogService)
        {
            _activity_ErrorLogService = activity_ErrorLogService;
        }

        /// <summary>
        /// Get paginated error logs
        /// </summary>
        /// <param name="paginationQuery"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<PagedResponse<ExceptionLog>>))]
        public async Task<IActionResult> GetPaginatedErrorLog([FromQuery]PaginationQuery paginationQuery)
        {
            var response = await _activity_ErrorLogService.GetPaginatedErrorLog(paginationQuery);
            try
            {
               var checkStringFromat =  DateTime.Parse(paginationQuery.SearchText).Date;
            }
            catch(Exception)
            {
                return BadRequest(new ResponseMessage { Message = "String not in a valid format" });
            }
            return Ok(response);
        }
    }
}
