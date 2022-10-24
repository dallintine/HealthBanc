using Application.API_ResponseModel.Paystack;
using Application.DTO;
using Application.AuditAndReport.AuditLog;
using Application.ViewModels;
using Application.ViewModels.Paystack;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Application.ViewModels.HealthInsured;
using DataAccess;
using Application.Services.Card;
using Application.Services.Activation_Deactivation;

namespace HealthBanc.Controllers.Insurance
{
    [Route("v{version:apiVersion}/api/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class TokenizationController : ControllerBase
    {
        private readonly AuditLogService _auditLogServices;
        private readonly IRepositoryWrapper _repoWrapper;
        private readonly Card_SubscriptionService _cardService;
        private readonly RestrictionService _restrictionService;
        public string ipAddress;
        public StringValues agent;

        public TokenizationController(IHttpContextAccessor accessor, AuditLogService auditLogServices,IRepositoryWrapper repoWrapper,Card_SubscriptionService cardService,
            RestrictionService restrictionService)
        {
            _auditLogServices = auditLogServices;
            _repoWrapper = repoWrapper;
            _cardService = cardService;
            _restrictionService = restrictionService;
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

                var chargeCardResponse = await _cardService.TokenizeCard(chargeCard, id);
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

                var otpResponse = await _cardService.SubmitOtp(otpViewModel, id);
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
            string email = User.FindFirst(ClaimTypes.Email)?.Value;

            int Id = int.Parse(userId);

            var cards = await _cardService.GetCards(Id,email);
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

                var changePrimaryCardResponse = await _cardService.ChangePrimaryCard(cardId, id);
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

                var deleteCardResponse = await _cardService.DeleteCard(cardId, id);
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

            var cancelSubscriptionResult = await _restrictionService.CancelSubscription(id, reason);
            if (cancelSubscriptionResult.Status)
            {
                return Ok(cancelSubscriptionResult);
            }
            return BadRequest(cancelSubscriptionResult);
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

            var reactivatewithPrimaryCardResponse = await _restrictionService.Reactivate(id);

            if (reactivatewithPrimaryCardResponse.Status)
            {
                return Ok(reactivatewithPrimaryCardResponse);
            }
            return BadRequest(reactivatewithPrimaryCardResponse);            
        }

        /// <summary>
        /// Deactivate referee
        /// </summary>
        /// <param name="insuranceProfileId"></param>
        /// <returns></returns>
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        public async Task<IActionResult> DeactivteReferee( int insuranceProfileId)
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int id = int.Parse(userId);
            var response = await _restrictionService.DeactivateReferee(id, insuranceProfileId);
            if (response.Status)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        /// <summary>
        /// Activate referee
        /// </summary>
        /// <param name="insuranceProfileId"></param>
        /// <returns></returns>
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        public async Task<IActionResult> ActivateRefereeWithPrimaryCard(int insuranceProfileId)
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int id = int.Parse(userId);
            var response = await _restrictionService.ActivateReferee(id, insuranceProfileId);
            if (response.Status)
            {
                return Ok(response);
            }
            return BadRequest(response);
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

            var response = await _restrictionService.DeactivateCompanyBeneficiaries(beneficiaryListViewModel, id);
            if (response.Status)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        /// <summary>
        /// Deactivate family member insurance profile
        /// </summary>
        /// <param name="insuranceProfileId"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        public async Task<IActionResult> DeactivateFamilyMember(int insuranceProfileId)
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int id = int.Parse(userId);

            var familyProfile = await _repoWrapper.FamilyProfile.GetExtendedFamilyDetails(id);
            var insuranceProfile = familyProfile.InsuranceUserProfiles.Where(x => x.Id == insuranceProfileId).FirstOrDefault();
            if(insuranceProfile != null)
            {
                var response =  await _restrictionService.DeactivateFamilyMember(insuranceProfile);
                if (response.Status)
                {
                    return Ok(response);
                }
                return BadRequest(response);
            }
            return NotFound(new ResponseMessage { Message = "You cannot delete current profile" });
            
        }

        /// <summary>
        /// Activayte family member insurance profile
        /// </summary>
        /// <param name="insuranceProfileId"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        public async Task<IActionResult> ActivateFamilyMember(int insuranceProfileId)
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int id = int.Parse(userId);

            var response = await _restrictionService.ActivateFamilyMember(id, insuranceProfileId);
            if (response.Status)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }


        /// <summary>
        /// Switch Payment method to card payment
        /// </summary>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("[action]")]
        public async Task<IActionResult> SwitchToCardPayment()
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int id = int.Parse(userId);
            var response = await _cardService.SwitchToCardPayment(id);
            if (response.Status) return Ok(response);
            return BadRequest(response);
        }
    }
}
