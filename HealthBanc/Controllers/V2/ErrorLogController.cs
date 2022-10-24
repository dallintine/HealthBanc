using Application.DTO;
using Application.Interfaces;
using Application.Services;
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
        private readonly ResponseHelper _responseHelper;

        public Activity_ErrorLogService _activity_ErrorLogService { get; }

        public ErrorLogController(Activity_ErrorLogService activity_ErrorLogService , IEncryptAndDecrypt encryptDecrypt,ResponseHelper responseHelper)
        {
            _activity_ErrorLogService = activity_ErrorLogService;
            _encryptDecrypt = encryptDecrypt;
            _responseHelper = responseHelper;
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
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var paginationQuery = JsonConvert.DeserializeObject<PaginationQuery>(decryptedString.Item2);

            var response = await _activity_ErrorLogService.GetPaginatedErrorLog(paginationQuery);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(response));
            try
            {
               var checkStringFromat =  DateTime.Parse(paginationQuery.SearchText).Date;
            }
            catch(Exception)
            {
                var data2 = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(new ResponseMessage { Message = "String not in a valid format" }));
                return BadRequest(data2);
            }
            return Ok(data);
        }
    }
}
