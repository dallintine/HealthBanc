using Application.API_ResponseModel.Paystack;
using Application.DTO;
using Application.HealthInsured_AxaMansard_Service.AuditAndReport.AuditLog;
using Application.Services.HealthInsured_AxaMansard.Insurance;
using Application.Services.Paystack;
using Application.ViewModels;
using DataAccess.HealthInsured_AxaMansard.Implementation;
using DataAccess.HealthInsured_AxaMansard.Interfaces;
using Domain.Models;
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
        private readonly PaystackService _paystackService;
        private readonly AuditLogService _auditLogServices;
        private readonly TokenizationService _tokkenizationService;
        private readonly IAxaMansardUserProfileRepository _mansardUserProfileRepository;
        private readonly IAxaMansardCompletionRepository _completionRepository;
        private readonly ILogger<PaystackController> _logger;
        private readonly IPaymentReferenceRepository _paymentReferenceRepository;
        public string ipAddress;
        public StringValues agent;

        public PaystackController(PaystackService paystackService,IHttpContextAccessor accessor, AuditLogService auditLogServices,TokenizationService tokkenizationService
            ,IAxaMansardUserProfileRepository mansardUserProfileRepository, IAxaMansardCompletionRepository completionRepository,ILogger<PaystackController> logger,
            IPaymentReferenceRepository paymentReferenceRepository)
        {
            _paystackService = paystackService;
            _auditLogServices = auditLogServices;
            _tokkenizationService = tokkenizationService;
            _mansardUserProfileRepository = mansardUserProfileRepository;
            _completionRepository = completionRepository;
            _logger = logger;
            _paymentReferenceRepository = paymentReferenceRepository;
            ipAddress = accessor.HttpContext.Connection.RemoteIpAddress.ToString();
            agent = accessor.HttpContext.Request.Headers["User-Agent"];
        }


        [HttpPost("[action]")]
        public IActionResult PaystackWebHook([FromBody]PaystackWebHookResponse webHookResponse)
        {
            string json = JsonConvert.SerializeObject(webHookResponse);
            _logger.LogCritical("Hit Pasytackwebhook.Successfully" + ipAddress + ":" + json);
            //if(webHookResponse.data.log.authentication == "open_url")
            //{
            //    var device = _auditLogServices.GetDevice(agent);
            //    var userAxamansardProfile = await _mansardUserProfileRepository.GetByUserIdAsync(paymentReference.UserId);
            //    var checkprofileComplete = await _completionRepository.GetCompletionStateByUserId(paymentReference.UserId);

            //    var tokenizationResponse =  new TokenizationResponse
            //    {
            //        Type = webHookResponse.data.authorization.card_type,
            //        LastDigit = webHookResponse.data.authorization.last4,
            //        AuthorizationCode = webHookResponse.data.authorization.authorization_code,
            //        Message = "Card was tokenize successfully",
            //        Status = true,
            //        Reference = paymentReference.Refernce,
            //        ResponseCode = 0
            //    };
            //    var processVerifyTransactionResponse = await _tokkenizationService.ProcessPaystackChargeCardResponse(tokenizationResponse, userAxamansardProfile,
            //        checkprofileComplete,paymentReference, paymentReference.Refernce, ipAddress, device);

            //    if(processVerifyTransactionResponse.Status == true)
            //    {
            //        return Ok();
            //    }
            //    else
            //    {
            //        return BadRequest();
            //    }
            //}
            return Ok();
        }        
    }
}
