using Application.DTO;
using Application.Interfaces;
using Application.Services;
using Application.ViewModels;
using AutoMapper;
using DataAccess;
using Domain.Models;
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
    public class LeadGeneratorController : ControllerBase
    {
        private readonly LeadGeneratorService _leadGenerator;
        private readonly IEncryptAndDecrypt _encryptDecrypt;
        private readonly ResponseHelper _responseHelper;

        public LeadGeneratorController(LeadGeneratorService leadGenerator, IEncryptAndDecrypt encryptDecrypt,ResponseHelper responseHelper)
        {
            _leadGenerator = leadGenerator;
            _encryptDecrypt = encryptDecrypt;
            _responseHelper = responseHelper;
        }

        /// <summary>
        /// Send Helium Notification
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public IActionResult SendHeliumNotification(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var heliumHealth = JsonConvert.DeserializeObject<HeliumHealthCollectionViewModel>(decryptedString.Item2);
            var response = _leadGenerator.SendHeliumNotification(heliumHealth);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(response));
            return Ok(data);
        }

        /// <summary>
        /// Send Health Finance Notification
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public async Task<IActionResult> SendHealthFinanceData(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var healthFinance = JsonConvert.DeserializeObject<HealthFinanceCollectionViewModel>(decryptedString.Item2);

            var response = await _leadGenerator.SendHealthFinanceData(healthFinance);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(response));
            if (response.Status)
            {
                return Ok(data);
            }
            return BadRequest(data);
        }

        /// <summary>
        /// Get paginated finance data
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<PagedResponse<HealthFinance>>))]
        //[Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        [HttpPost("[action]")]
        public async Task<IActionResult> GetPaginatedFinanceData(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var model = JsonConvert.DeserializeObject<GetPaginatedFinanceDataViewModel>(decryptedString.Item2);

            var response = await _leadGenerator.GetPaginatedHealthFinanceData(model, model.StartDate, model.EndDate);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(response));
            return Ok(data);
        }

        [HttpPost("[action]")]
        public IActionResult DowloadFinaceExcelData(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(new
                ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var model = JsonConvert.DeserializeObject<DowloadFinaceExcelDataViewModel>(decryptedString.Item2);

            var response = _leadGenerator.DowloadFinaceExcelData(model.StartDate, model.EndDate);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(response));
            if (response.Status)
            {
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                string fileName = "healthfinance.xlsx";

                var content = response.Data as byte[];
                return File(content, contentType, fileName);
            }
            return BadRequest(data);          
        }
    }
}
