using AutoMapper;
using HealthBanc.Infrastructure.Mail;
using HealthBanc.Response;
using HealthBanc.ViewModels;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using static HealthBanc.Infrastructure.Mail.EmailSender;

namespace HealthBanc.Controllers.BackendAdmin
{
    [Route("v1/api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly IEmailSender _emailSender;
        private readonly IMapper _mapper;

        public NotificationController(IEmailSender emailSender,IMapper mapper)
        {
            _emailSender = emailSender;
            _mapper = mapper;
        }

        [HttpPost("[action]")]
        public  IActionResult SendHeliumNotification(HeliumHealthCollectionViewModel heliumHealth)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var @object = _mapper.Map<HelloEmail>(heliumHealth);
                    _emailSender.SendEmailWithObject("Oluwaseunayo.Lojede@sterling.ng", "d-ae6ac5d73c714e3896c7011b5276c2b5", @object);
                    return Ok(new ResponseMessage{ Status = true, Message = "Notification was sent successfully" });
                }
                catch(Exception ex)
                {
                    return BadRequest(new ResponseMessage{ Message = "An error occurred while trying to send notification" });
                }               
            }
            var errors = new List<string>();
            var errorList = ModelState.Values.SelectMany(m => m.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            foreach (var error in errorList)
            {
                errors.Add(error);
            }
            return BadRequest(new ResponseMessage(){ Data = errors });
        }
    }
}