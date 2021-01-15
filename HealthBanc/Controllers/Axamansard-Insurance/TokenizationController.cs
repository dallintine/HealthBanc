using Application.API_ResponseModel.Paystack;
using Application.DTO;
using Application.HealthInsured_AxaMansard_Service.AuditAndReport.AuditLog;
using Application.Services.HealthInsured_AxaMansard.Insurance;
using Application.ViewModels;
using Application.ViewModels.Paystack;
using AutoMapper;
using DataAccess.HealthInsured_AxaMansard.Interfaces;
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

namespace HealthBanc.Controllers.Axamansard_Insurance
{
    [Route("v1/api/[controller]")]
    [ApiController]
    public class TokenizationController : ControllerBase
    {
        private readonly TokenizationService _tokenizationService;
        private readonly AuditLogService _auditLogServices;
        private readonly IAxaMansardUserProfileRepository _mansardUserProfileRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<TokenizationController> _logger;
        public string ipAddress;
        public StringValues agent;

        public TokenizationController(TokenizationService tokenizationService,IHttpContextAccessor accessor, AuditLogService auditLogServices,
            IAxaMansardUserProfileRepository mansardUserProfileRepository,IMapper mapper,ILogger<TokenizationController>logger)
        {
            _tokenizationService = tokenizationService;
            _auditLogServices = auditLogServices;
            _mansardUserProfileRepository = mansardUserProfileRepository;
            _mapper = mapper;
            _logger = logger;
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
        [EnableCors("Cors")]
        [HttpPost("[action]")]
        public async Task<IActionResult> ChargeCard(ChargeCardViewModel chargeCard)
        {
            if (ModelState.IsValid)
            {
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int id = int.Parse(userId);
                var device = _auditLogServices.GetDevice(agent);

                var chargeCardResponse = await _tokenizationService.TokenizeCard(chargeCard, id,ipAddress,device);
                if (chargeCardResponse.Status && chargeCardResponse.ResponseCode == 20)
                {
                    var tokenization = chargeCardResponse.Data as TokenizationResponse;
                    return Redirect("https://www.google.com");
                }
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

        //[EnableCors("Cors")]
        //[HttpGet("[action]")]
        //public ActionResult SendUrl()
        //{
        //    return Ok();
        //}

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("[action]")]
        public async Task<IActionResult> SubmitOtp(SetOtpViewModel otpViewModel)
        {
            if (ModelState.IsValid)
            {
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int id = int.Parse(userId);
                var device = _auditLogServices.GetDevice(agent);

                var otpResponse = await _tokenizationService.SubmitOtp(otpViewModel, id,ipAddress,device);
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
        /// Get subscriber cards
        /// </summary>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<List<CardDTO>>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage<List<CardDTO>>))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage<List<CardDTO>>))]
        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("[action]")]
        public async Task<IActionResult> GetCards()
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            var cards = await _mansardUserProfileRepository.GetByUserIdAsync(Id);
            if (cards != null)
            {
                if (cards.Cards.Count > 0)
                {
                    var cardDTO = _mapper.Map<List<DebitCard>, List<CardDTO>>(cards.Cards);
                    return Ok(new ResponseMessage<List<CardDTO>> { Data = cardDTO, Status = true, Message = "Cards was fetchd successfully" });
                }
                var emptyCardDTO = new List<CardDTO>();
                return Ok(new ResponseMessage<List<CardDTO>> { Data = emptyCardDTO, Message = "User has no card, Kindly add a card", Status = true });
            }
            return NotFound(new ResponseMessage<List<CardDTO>> { Status = false, Message = "User was not found" });
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("[action]")]
        public async Task<IActionResult> ChangePrimaryCard(int cardId)
        {
            if (ModelState.IsValid)
            {
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int id = int.Parse(userId);
                var device = _auditLogServices.GetDevice(agent);

                var changePrimaryCardResponse = await _tokenizationService.ChangePrimaryCard(cardId, id, ipAddress, device);
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

        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("[action]")]
        public async Task<IActionResult> DeleteCard(int cardId)
        {
            if (ModelState.IsValid)
            {
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int id = int.Parse(userId);
                var device = _auditLogServices.GetDevice(agent);

                var deleteCardResponse = await _tokenizationService.DeleteCard(cardId, id, ipAddress, device);
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

        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("[action]")]
        public async Task<IActionResult> ReactivateWithPresentPrimaryCard()
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int id = int.Parse(userId);
            var device = _auditLogServices.GetDevice(agent);

            var reactivatewithPrimaryCardResponse = await _tokenizationService.ReactivateWithPresentPrimaryCard(id, ipAddress, device);

            if (reactivatewithPrimaryCardResponse.Status)
            {
                return Ok(reactivatewithPrimaryCardResponse);
            }
            return BadRequest(reactivatewithPrimaryCardResponse);            
        }
    }
}
