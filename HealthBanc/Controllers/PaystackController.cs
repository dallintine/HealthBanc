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
using Application.Services.HealthInsured;

namespace HealthBanc.Controllers
{
    [Route("v1/api/[controller]")]
    [ApiController]
    public class PaystackController : ControllerBase
    {
        private readonly InsurancePSWebHookService _insurancePSWebHookService;
        private readonly ILogger<PaystackController> _logger;
        public string ipAddress;
        public StringValues agent;

        public PaystackController(InsurancePSWebHookService insurancePSWebHookService, IHttpContextAccessor accessor,ILogger<PaystackController> logger)
        {
            _insurancePSWebHookService = insurancePSWebHookService;
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
        public async Task<IActionResult> PaystackWebHook([FromBody]PaystackWebHookResponse webHookResponse)
        {
            var paystackIpaddress = new List<string>()
            {
                "52.49.173.169","52.214.14.220","52.31.139.75"
            };
            if (paystackIpaddress.Contains(ipAddress))
            {               
                var amount = webHookResponse.data.amount / 100;
                _logger.LogInformation($"event {webHookResponse.@event} | email : {webHookResponse.data.customer.email} | refeence : {webHookResponse.data.reference} " +
                    $"| authcode: {webHookResponse.data.authorization.authorization_code} | last4 : {webHookResponse.data.authorization.last4} " +
                    $"| type : {webHookResponse.data.authorization.card_type} |amount :{amount} ");
                await _insurancePSWebHookService.ProcessPaystackWebHook(webHookResponse.@event, webHookResponse.data.customer.email, webHookResponse.data.reference,
                    webHookResponse.data.authorization.authorization_code, webHookResponse.data.authorization.last4, webHookResponse.data.authorization.card_type, amount.ToString());
                return Ok();
            }
            return Ok();
        }          
    }
}
