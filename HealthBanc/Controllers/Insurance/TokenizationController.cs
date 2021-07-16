using Application.API_ResponseModel.Paystack;
using Application.DTO;
using Application.AuditAndReport.AuditLog;
using Application.ViewModels;
using Application.ViewModels.Paystack;
using AutoMapper;
using DataAccess.HealthInsured.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Application.ViewModels.HealthInsured;
using Application.Services.HealthInsured_AxaMansard.Insurance;
using Application.Services;

namespace HealthBanc.Controllers.Insurance
{
    [Route("v1/api/[controller]")]
    [ApiController]
    public class TokenizationController : ControllerBase
    {
        private readonly TokenizationService _tokenizationService;
        private readonly AuditLogService _auditLogServices;
        private readonly IBSIntegrationService _iBSIntegrationService;
        public string ipAddress;
        public StringValues agent;

        public TokenizationController(TokenizationService tokenizationService,IHttpContextAccessor accessor, AuditLogService auditLogServices,IBSIntegrationService iBSIntegrationService)
        {
            _tokenizationService = tokenizationService;
            _auditLogServices = auditLogServices;
            _iBSIntegrationService = iBSIntegrationService;
            ipAddress = accessor.HttpContext.Connection.RemoteIpAddress.ToString();
            agent = accessor.HttpContext.Request.Headers["User-Agent"];
        }

        /// <summary>
        /// Charge user card for tokenization process
        /// </summary>
        /// <param name="chargeCard"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<TokenizationResponse>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage<TokenizationResponse>))]
        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("[action]")]
        public async Task<IActionResult> ChargeCard(ChargeCardViewModel chargeCard)
        {
            if (ModelState.IsValid)
            {
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int id = int.Parse(userId);
                var device = _auditLogServices.GetDevice(agent);

                var chargeCardResponse = await _tokenizationService.TokenizeCard(chargeCard, id);
                if (chargeCardResponse.Status)
                {
                    return Ok(chargeCardResponse);
                }
                return BadRequest(chargeCardResponse);
            }
            //return validation errors
            var errors = new List<string>();
            var errorList = ModelState.Values.SelectMany(m => m.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            foreach (var error in errorList)
            {
                errors.Add(error);
            }
            return BadRequest(new ResponseMessage { Data = errors, Message = errors.FirstOrDefault().ToString() });
        }

        /// <summary>
        /// Action to submit OTP during tokenization process
        /// </summary>
        /// <param name="otpViewModel"></param>
        /// <returns></returns>
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<TokenizationResponse>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage<TokenizationResponse>))]
        [HttpPost("[action]")]
        public async Task<IActionResult> SubmitOtp(SetOtpViewModel otpViewModel)
        {
            if (ModelState.IsValid)
            {
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int id = int.Parse(userId);

                var otpResponse = await _tokenizationService.SubmitOtp(otpViewModel, id);
                if (otpResponse.Status)
                {
                    return Ok(otpResponse);
                }
                return BadRequest(otpResponse);
            }
            //return validation errors
            var errors = new List<string>();
            var errorList = ModelState.Values.SelectMany(m => m.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            foreach (var error in errorList)
            {
                errors.Add(error);
            }
            return BadRequest(new ResponseMessage { Data = errors, Message = errors.FirstOrDefault().ToString() });
        }

        /// <summary>
        /// Cancel Subscription
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("[action]")]
        public async Task<IActionResult> CancelSubscription(string reason)
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int id = int.Parse(userId);

            var cancelSubscriptionResult = await _tokenizationService.CancelSubscription(id, reason);
            if (cancelSubscriptionResult.Status)
            {
                return Ok(cancelSubscriptionResult);
            }
            return BadRequest(cancelSubscriptionResult);
        }

        /// <summary>
        /// Get debit card for both coporate and individual health insured users
        /// </summary>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<List<CardDTO>>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("[action]")]
        public async Task<IActionResult> GetCards()
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            var cards = await _tokenizationService.GetCards(Id);
            if (cards.Status)
            {
                return Ok(cards);
            }
            return BadRequest(cards);
        }

        /// <summary>
        /// Change primary card debit card for both corporate and individual healthinsured users
        /// </summary>
        /// <param name="cardId"></param>
        /// <returns></returns>
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        public async Task<IActionResult> ChangePrimaryCard(int cardId)
        {
            if (ModelState.IsValid)
            {
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int id = int.Parse(userId);

                var changePrimaryCardResponse = await _tokenizationService.ChangePrimaryCard(cardId, id);
                if (changePrimaryCardResponse.Status)
                {
                    return Ok(changePrimaryCardResponse);
                }
                else if(!changePrimaryCardResponse.Status && changePrimaryCardResponse.ResponseCode == 12)
                {
                    return NotFound(changePrimaryCardResponse);
                }
                return BadRequest(changePrimaryCardResponse);
            }
            //return validation errors
            var errors = new List<string>();
            var errorList = ModelState.Values.SelectMany(m => m.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            foreach (var error in errorList)
            {
                errors.Add(error);
            }
            return BadRequest(new ResponseMessage { Data = errors, Message = errors.FirstOrDefault().ToString() });
        }

        /// <summary>
        /// Delete Debit Card for both corporate and individual healthinsured users
        /// </summary>
        /// <param name="cardId"></param>
        /// <returns></returns>
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        public async Task<IActionResult> DeleteCard(int cardId)
        {
            if (ModelState.IsValid)
            {
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int id = int.Parse(userId);

                var deleteCardResponse = await _tokenizationService.DeleteCard(cardId, id);
                if (deleteCardResponse.Status)
                {
                    return Ok(deleteCardResponse);
                }
                else if(!deleteCardResponse.Status && deleteCardResponse.ResponseCode == 12)
                {
                    return NotFound(deleteCardResponse);
                }
                return BadRequest(deleteCardResponse);
            }
            //return validation errors
            var errors = new List<string>();
            var errorList = ModelState.Values.SelectMany(m => m.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            foreach (var error in errorList)
            {
                errors.Add(error);
            }
            return BadRequest(new ResponseMessage { Data = errors, Message = errors.FirstOrDefault().ToString() });
        }

        /// <summary>
        /// Reactivate Healthinsured insurance service for individual users
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        public async Task<IActionResult> ReactivateWithPresentPrimaryCard()
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int id = int.Parse(userId);

            var reactivatewithPrimaryCardResponse = await _tokenizationService.ReactivateWithPresentPrimaryCard(id);

            if (reactivatewithPrimaryCardResponse.Status)
            {
                return Ok(reactivatewithPrimaryCardResponse);
            }
            return BadRequest(reactivatewithPrimaryCardResponse);            
        }

        /// <summary>
        /// Action to Deactivate beneficiairies  under a coporate organisation.
        /// </summary>
        /// <param name="beneficiaryListViewModel"></param>
        /// <returns></returns>
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public async Task<IActionResult> DeactivateCompanyBeneficiary(BeneficiaryListViewModel beneficiaryListViewModel)
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int id = int.Parse(userId);

            var response = await _tokenizationService.DeactivateCompanyBeneficiaries(beneficiaryListViewModel, id);
            if (response.Status)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        /// <summary>
        /// Get HMO invoice details
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "Super-Administrator")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        public IActionResult GetHMOInvoiceDetails()
        {
            var response = _tokenizationService.GetHMOInvoiceDetailsForMonthEnd();
            return Ok(response);
        }

        [Authorize(Roles = "Super-Administrator")]
        [HttpGet("[action]")]
        public async Task<IActionResult> FixPaymentError(string reference, string amount)
        {
            await _tokenizationService.FitPaymentError(reference, amount);
            return Ok();
        }

        /// <summary>
        /// Perform sterling intra bank transfer
        /// </summary>
        /// <param name="toAccount"></param>
        /// <param name="fromAccount"></param>
        /// <param name="amount"></param>
        /// <param name="des"></param>
        /// <returns></returns>
        [Authorize(Roles = "Super-Administrator")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        public async Task<IActionResult> SendMoney(string toAccount,string fromAccount,string amount,string des)
        {
            var res = await _iBSIntegrationService.SterlingBankIntraBank(decimal.Parse(amount), toAccount, fromAccount, des, "NG0020032");
            return Ok(res);
        }

        [Authorize(Roles = "Super-Administrator")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        public async Task<IActionResult> ProcessFailedHMOPayment(int Id)
        {
            await _tokenizationService.ProcessFailedHMOPayment(Id);
            return Ok();
        }
    }
}
