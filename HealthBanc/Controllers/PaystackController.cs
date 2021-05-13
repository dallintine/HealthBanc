using Application.API_ResponseModel.Paystack;
using Application.DTO;
using Application.AuditAndReport.AuditLog;
using Application.Services.Paystack;
using Application.ViewModels;
using DataAccess.HealthInsured.Implementation;
using DataAccess.HealthInsured.Interfaces;
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
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Linq;
using System.Collections.Generic;
using Application.Services.HealthInsured_AxaMansard.Insurance;
using System.Threading;

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
            var paystackIpaddress = new List<string>()
            {
                "52.49.173.169","52.214.14.220","52.31.139.75"
            };
            if (paystackIpaddress.Contains(ipAddress))
            {
                _logger.LogCritical("Hit Pasytackwebhook.Successfully" + ipAddress + " : " + DateTime.Now.ToLongDateString() + " : "+ webHookResponse.data.customer.email + " : " + webHookResponse.data.amount.ToString());

                var amount = webHookResponse.data.amount / 100;
                Thread.Sleep(60000);
                BackgroundJob.Enqueue(() => _tokenizationService.ProcessPaystackWebHook(webHookResponse.@event, webHookResponse.data.customer.email, webHookResponse.data.reference,
                    webHookResponse.data.authorization.authorization_code, webHookResponse.data.authorization.last4, webHookResponse.data.authorization.card_type, amount.ToString()));
                return Ok();
            }
            return Ok();
        }          
    }
}
