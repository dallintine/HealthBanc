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
using Application.Services.HealthInsured;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Linq;

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
            //String key = "YOUR_SECRET_KEY"; //replace with your paystack secret_key
            //String jsonInput = JsonConvert.SerializeObject(webHookResponse); ; //the json input
            //String inputString = Convert.ToString(new JValue(jsonInput));
            //String result = "";
            //byte[] secretkeyBytes = Encoding.UTF8.GetBytes(key);
            //byte[] inputBytes = Encoding.UTF8.GetBytes(inputString);
            //using (var hmac = new HMACSHA512(secretkeyBytes))
            //{
            //    byte[] hashValue = hmac.ComputeHash(inputBytes);
            //    result = BitConverter.ToString(hashValue).Replace("-", string.Empty); ;
            //}
            try
            {
                HttpRequestMessage request = new HttpRequestMessage();
                var x = request.Headers.GetValues("X-Paystack-Signature"); var y = x.FirstOrDefault();
                _logger.LogCritical(y);
            }
            catch(Exception ex)
            {

            }
            
            //String xpaystackSignature = ; //put in the request's header value for x-paystack-signature

            //if (result.ToLower().Equals(xpaystackSignature))
            //{
            //    // you can trust the event, it came from paystack
            //    // respond with the http 200 response immediately before attempting to process the response
            //    //retrieve the request body, and deliver value to the customer
            //}
            //else
            //{
            //    // this isn't from Paystack, ignore it
            //}
            _logger.LogCritical("Hit Pasytackwebhook.Successfully" + ipAddress + ":" );
            _logger.LogCritical("Hit Pasytackwebhook.Successfully" +
                 webHookResponse.data.customer.email);

            BackgroundJob.Enqueue(() => _tokenizationService.ProcessPaystackWebHook(webHookResponse.@event, webHookResponse.data.customer.email, webHookResponse.data.reference,
                webHookResponse.data.authorization.authorization_code, webHookResponse.data.authorization.last4, webHookResponse.data.authorization.card_type,webHookResponse.data.amount.ToString()));
            return Ok();
        }          
    }
}
