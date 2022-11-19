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

        public LeadGeneratorController(LeadGeneratorService leadGenerator, IEncryptAndDecrypt encryptDecrypt)
        {
            _leadGenerator = leadGenerator;
            _encryptDecrypt = encryptDecrypt;
        }

        /// <summary>
        /// Send Helium Notification
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public async Task<IActionResult> SendHeliumNotification(EncryptedModel encryptedModel)
        {
            if (ModelState.IsValid)
            {
                var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
                if (!decryptedString.Item1) return BadRequest(new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 });
                var heliumHealth = JsonConvert.DeserializeObject<HeliumHealthCollectionViewModel>(decryptedString.Item2);

                var response = await _leadGenerator.SendHeliumNotification(heliumHealth);
                return Ok(response);
               
            }
            var errors = new List<string>();
            var errorList = ModelState.Values.SelectMany(m => m.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            foreach (var error in errorList)
            {
                errors.Add(error);
            }
            return BadRequest(new ResponseMessage() { Data = errors, Message = errors.FirstOrDefault() });
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
            if (ModelState.IsValid)
            {
                var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
                if (!decryptedString.Item1) return BadRequest(new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 });
                var healthFinance = JsonConvert.DeserializeObject<HealthFinanceCollectionViewModel>(decryptedString.Item2);

                var response = await _leadGenerator.SendHealthFinanceData(healthFinance);
                if (response.Status)
                {
                    return Ok(response);
                }
                return BadRequest(response);
            }
            var errors = new List<string>();
            var errorList = ModelState.Values.SelectMany(m => m.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            foreach (var error in errorList)
            {
                errors.Add(error);
            }
            return BadRequest(new ResponseMessage() { Data = errors, Message = errors.FirstOrDefault() });
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
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 });
            var model = JsonConvert.DeserializeObject<GetPaginatedFinanceDataViewModel>(decryptedString.Item2);


            var response = await _leadGenerator.GetPaginatedHealthFinanceData(model, model.StartDate, model.EndDate);
            return Ok(response);
        }

        [HttpPost("[action]")]
        public IActionResult DowloadFinaceExcelData(EncryptedModel encryptedModel)
        {
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 });
            var model = JsonConvert.DeserializeObject<DowloadFinaceExcelDataViewModel>(decryptedString.Item2);

            var response = _leadGenerator.DowloadFinaceExcelData(model.StartDate, model.EndDate);
            if (response.Status)
            {
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                string fileName = "healthfinance.xlsx";

                var content = response.Data as byte[];
                return File(content, contentType, fileName);
            }
            return BadRequest(response);
           
        }
    }
}
