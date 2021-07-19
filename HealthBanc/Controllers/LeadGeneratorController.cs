using Application.DTO;
using Application.Interfaces;
using Application.Services;
using Application.ViewModels;
using AutoMapper;
using DataAccess;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Controllers
{
    [Route("v1/api/[controller]")]
    [ApiController]
    public class LeadGeneratorController : ControllerBase
    {
        private readonly LeadGeneratorService _leadGenerator;

        public LeadGeneratorController(LeadGeneratorService leadGenerator)
        {
            _leadGenerator = leadGenerator;
        }

        /// <summary>
        /// Send Helium Notification
        /// </summary>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public IActionResult SendHeliumNotification(HeliumHealthCollectionViewModel heliumHealth)
        {
            if (ModelState.IsValid)
            {
                var response = _leadGenerator.SendHeliumNotification(heliumHealth);
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
        /// <param name="healthFinance"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public async Task<IActionResult> SendHealthFinanceData(HealthFinanceCollectionViewModel healthFinance)
        {
            if (ModelState.IsValid)
            {
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
        /// <param name="paginationQuery"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<PagedResponse<HealthFinance>>))]
        //[Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        [HttpPost("[action]")]
        public async Task<IActionResult> GetPaginatedFinanceData([FromQuery] PaginationQuery paginationQuery,[FromQuery] DateTime? startDate,[FromQuery] DateTime? endDate)
        {
            var response = await _leadGenerator.GetPaginatedHealthFinanceData(paginationQuery,startDate,endDate);
            return Ok(response);
        }

        [HttpGet("[action]")]
        public IActionResult DowloadFinaceExcelData([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {      
            var response = _leadGenerator.DowloadFinaceExcelData(startDate, endDate);
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
