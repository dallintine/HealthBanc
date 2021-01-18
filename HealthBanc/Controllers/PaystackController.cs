using Application.API_ResponseModel.Paystack;
using Application.DTO;
using Application.HealthInsured_AxaMansard_Service.AuditAndReport.AuditLog;
using Application.Services.HealthInsured_AxaMansard.Insurance;
using Application.Services.Paystack;
using Application.ViewModels;
using DataAccess.HealthInsured_AxaMansard.Implementation;
using DataAccess.HealthInsured_AxaMansard.Interfaces;
using Domain.Models;
using Hangfire;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace HealthBanc.Controllers
{
    [Route("v1/api/[controller]")]
    [ApiController]
    public class PaystackController : ControllerBase
    {
        private readonly TokenizationService _tokenizationService;
        private readonly ILogger<PaystackController> _logger;
        public string ipAddress;
        public StringValues agent;

        public PaystackController(TokenizationService tokenizationService, IHttpContextAccessor accessor,ILogger<PaystackController> logger)
        {
            _tokenizationService = tokenizationService;
            _logger = logger;
            ipAddress = accessor.HttpContext.Connection.RemoteIpAddress.ToString();
            agent = accessor.HttpContext.Request.Headers["User-Agent"];
        }

        /// <summary>
        /// WebHokk to listen to charge success event from paystack
        /// </summary>
        /// <param name="webHookResponse"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        public IActionResult PaystackWebHook([FromBody]PaystackWebHookResponse webHookResponse)
        {
            string json = JsonConvert.SerializeObject(webHookResponse);
            _logger.LogCritical("Hit Pasytackwebhook.Successfully" + ipAddress + ":" );
            _logger.LogCritical("Hit Pasytackwebhook.Successfully" + webHookResponse.data.authorization.authorization_code + " "+
                 webHookResponse.data.customer.email);

            BackgroundJob.Enqueue(() => _tokenizationService.ProcessPaystackWebHook(webHookResponse.@event, webHookResponse.data.customer.email, webHookResponse.data.reference,
                webHookResponse.data.authorization.authorization_code, webHookResponse.data.authorization.last4, webHookResponse.data.authorization.card_type,ipAddress, webHookResponse.data.amount.ToString()));
            return Ok();
        }          
    }
}
