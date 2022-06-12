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
using Microsoft.Extensions.Options;
using Application.Helpers;

namespace HealthBanc.Controllers
{
    [Route("v1/api/[controller]")]
    [ApiController]
    public class PaystackController : ControllerBase
    {
        private readonly InsurancePSWebHookService _insurancePSWebHookService;
        private readonly ILogger<PaystackController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Paystack _paystackOptions;
        public string ipAddress;
        public StringValues agent;

        public PaystackController(InsurancePSWebHookService insurancePSWebHookService, IHttpContextAccessor accessor,ILogger<PaystackController> logger,
            IOptions<Paystack> paystackOptions,IHttpClientFactory httpClientFactory)
        {
            _insurancePSWebHookService = insurancePSWebHookService;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _paystackOptions = paystackOptions.Value;
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
            _logger.LogInformation($"Paystack Webhook Notification [Payload : {JsonConvert.SerializeObject(webHookResponse)}]\n");
            try
            {
                var paystackIpaddress = new List<string>()
                {
                    "52.49.173.169","52.214.14.220","52.31.139.75"
                };
                if (paystackIpaddress.Contains(ipAddress))
                {
                    var fintechPrefix = _paystackOptions.Healthinsured_FintechWebhookPrefix;
                    if (webHookResponse.data.reference.StartsWith(fintechPrefix))
                    {
                        if(webHookResponse.@event == "charge.success")
                        {
                            BackgroundJob.Enqueue(() => ForwardWebHookNotification(webHookResponse));
                        }                       
                    }
                    else
                    {
                        var amount = webHookResponse.data.amount / 100;
                        BackgroundJob.Enqueue(() => _insurancePSWebHookService.ProcessPaystackWebHook(webHookResponse.@event, webHookResponse.data.customer.email, webHookResponse.data.reference,
                            webHookResponse.data.authorization.authorization_code, webHookResponse.data.authorization.last4, webHookResponse.data.authorization.card_type, amount.ToString()));
                    }                    
                }
                return Ok();
            }
            catch(Exception ex)
            {
                _logger.LogCritical($"Exception occured when paystack notification was received [Exception :{ex}]\n {ex.ToString()}\n");
                return Ok();
            }
        }

        [AutomaticRetry(Attempts = 0)]
        public  async Task ForwardWebHookNotification(PaystackWebHookResponse webHookResponse)
        {
            var client = _httpClientFactory.CreateClient("HealthInsured_Fintech");
            var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, _paystackOptions.Healthinsured_FintechWebhookURL));
            //if (!response.IsSuccessStatusCode)
            //{
            //    BackgroundJob.Enqueue(() => ForwardWebHookNotification(webHookResponse));
            //}
        }
    }
}
