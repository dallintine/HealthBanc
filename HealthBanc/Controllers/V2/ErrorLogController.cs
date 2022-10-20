using Application.DTO;
using Application.Interfaces;
using Application.Services.AuditAndReport;
using DataAccess;
using Domain.Models.ExceptionLog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Controllers.V2
{
    [Route("v{version:apiVersion}/api/[controller]")]
    [ApiController]
    [ApiVersion("2.0")]
    public class ErrorLogController : ControllerBase
    {
        private readonly IEncryptAndDecrypt _encryptDecrypt;

        public Activity_ErrorLogService _activity_ErrorLogService { get; }

        public ErrorLogController(Activity_ErrorLogService activity_ErrorLogService , IEncryptAndDecrypt encryptDecrypt)
        {
            _activity_ErrorLogService = activity_ErrorLogService;
            _encryptDecrypt = encryptDecrypt;
        }

        /// <summary>
        /// Get paginated error logs
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<PagedResponse<ExceptionLog>>))]
        public async Task<IActionResult> GetPaginatedErrorLog(EncryptedModel encryptedModel)
        {
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 });
            var paginationQuery = JsonConvert.DeserializeObject<PaginationQuery>(decryptedString.Item2);

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
