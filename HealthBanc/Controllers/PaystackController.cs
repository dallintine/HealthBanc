using Application.API_ResponseModel.Paystack;
using Application.DTO;
using Application.HealthInsured_AxaMansard_Service.AuditAndReport.AuditLog;
using Application.Services.HealthInsured_AxaMansard.Insurance;
using Application.Services.Paystack;
using DataAccess.HealthInsured_AxaMansard.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
        public string ipAddress;
        public StringValues agent;

        public PaystackController(PaystackService paystackService,IHttpContextAccessor accessor, AuditLogService auditLogServices,TokenizationService tokkenizationService
            ,IAxaMansardUserProfileRepository mansardUserProfileRepository, IAxaMansardCompletionRepository completionRepository,ILogger<PaystackController> logger)
        {
            _paystackService = paystackService;
            _auditLogServices = auditLogServices;
            _tokkenizationService = tokkenizationService;
            _mansardUserProfileRepository = mansardUserProfileRepository;
            _completionRepository = completionRepository;
            _logger = logger;
            ipAddress = accessor.HttpContext.Connection.RemoteIpAddress.ToString();
            agent = accessor.HttpContext.Request.Headers["User-Agent"];
        }

        [HttpPost("[action]")]
        public ActionResult PaystackWebHook(PaystackWebHookResponse webHookResponse)
        {
            _logger.LogWarning("Hit Pasytackwebhook.Successfully"+webHookResponse.data.status);
            return Ok(new ResponseMessage { Data = webHookResponse, Status = true });
        }

        //[HttpGet("[action]")]
        //public async Task<IActionResult> PaystackCallback(string reference)
        //{
        //    _logger.LogWarning("Hit PaystackCallback");
        //    string userId = User.FindFirst(ClaimTypes.Name)?.Value;
        //    int id = int.Parse(userId);
        //    var device = _auditLogServices.GetDevice(agent);

        //    var userAxamansardProfile = await _mansardUserProfileRepository.GetByUserIdAsync(id);
        //    var checkprofileComplete = await _completionRepository.GetCompletionStateByUserId(id);

        //    var verifyTransaction = await _paystackService.VerifyTransaction(reference);
        //    var processVerifyTransactionResponse = await _tokkenizationService.ProcessPaystackChargeCardResponse(verifyTransaction, userAxamansardProfile,
        //        checkprofileComplete, reference, ipAddress, device);

        //    if(processVerifyTransactionResponse.Status == true && !checkprofileComplete.TokenizationCompleted)
        //    {
        //        return Redirect("https://pharmmall.azurewebsites.net/health_profile");
        //    }
        //    else if(processVerifyTransactionResponse.Status == true && checkprofileComplete.TokenizationCompleted)
        //    {
        //        return Redirect("https://pharmmall.azurewebsites.net/health_card");
        //    }
        //    return Redirect("https://pharmmall.azurewebsites.net/health_care_provider");
        //}
    }
}
