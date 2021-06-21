using Application.DTO;
using Application.Interfaces;
using Application.ViewModels;
using AutoMapper;
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
        private readonly IMapper _mapper;
        private readonly IEmailSender _emailSender;

        public LeadGeneratorController(IMapper mapper, IEmailSender emailSender)
        {
            _mapper = mapper;
            _emailSender = emailSender;
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
                _emailSender.SendHeliumNotification("Helium Notification", heliumHealth.HealthServiceProviderName, heliumHealth.HealthServiveProviderType, heliumHealth.PhoneNumber,
                    heliumHealth.EmailAddress);
                return Ok(new ResponseMessage { Status = true, Message = "Notification was sent successfully" });
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
        public IActionResult SendHealthFinanceData(HealthFinanceCollectionViewModel healthFinance)
        {
            if (ModelState.IsValid)
            {
                var emails = new List<string>
                {
                    "Effiong.Effiong@sterling.ng" , "Hassan.Hassan@sterling.ng"
                };
                _emailSender.SendHealthFinanceNotification("HealthFinance Notification",healthFinance ,emails);
                return Ok(new ResponseMessage { Status = true, Message = "Notification was sent successfully" });
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
    }
}
