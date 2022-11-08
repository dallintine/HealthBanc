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
using Newtonsoft.Json;
using Application.Interfaces;
using UAParser;

namespace HealthBanc.Controllers.V2.Insurance
{
    [Route("v{version:apiVersion}/api/[controller]")]
    [ApiController]
    [ApiVersion("2.0")]
    public class TokenizationController : ControllerBase
    {
        private readonly AuditLogService _auditLogServices;
        private readonly IRepositoryWrapper _repoWrapper;
        private readonly Card_SubscriptionService _cardService;
        private readonly RestrictionService _restrictionService;
        private readonly IEncryptAndDecrypt _encryptDecrypt;
        public string ipAddress;
        public StringValues agent;

        public TokenizationController(IHttpContextAccessor accessor, AuditLogService auditLogServices,IRepositoryWrapper repoWrapper,Card_SubscriptionService cardService,
            RestrictionService restrictionService , IEncryptAndDecrypt encryptDecrypt)
        {
            _auditLogServices = auditLogServices;
            _repoWrapper = repoWrapper;
            _cardService = cardService;
            _restrictionService = restrictionService;
            _encryptDecrypt = encryptDecrypt;
            ipAddress = accessor.HttpContext.Connection.RemoteIpAddress.ToString();
            agent = accessor.HttpContext.Request.Headers["User-Agent"];
        }

        /// <summary>
        /// Charge user card for tokenization process
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<TokenizationResponse>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage<TokenizationResponse>))]
        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("[action]")]
        public async Task<IActionResult> ChargeCard(EncryptedModel encryptedModel)
        {
            if (ModelState.IsValid)
            {
                var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
                if (!decryptedString.Item1) return BadRequest(new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 });
                var chargeCard = JsonConvert.DeserializeObject<ChargeCardViewModel>(decryptedString.Item2);

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
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<TokenizationResponse>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage<TokenizationResponse>))]
        [HttpPost("[action]")]
        public async Task<IActionResult> SubmitOtp(EncryptedModel encryptedModel)
        {
            if (ModelState.IsValid)
            {
                var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
                if (!decryptedString.Item1) return BadRequest(new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 });
                var otpViewModel = JsonConvert.DeserializeObject<SetOtpViewModel>(decryptedString.Item2);

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
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public async Task<IActionResult> ChangePrimaryCard(EncryptedModel encryptedModel)
        {
            if (ModelState.IsValid)
            {
                var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
                if (!decryptedString.Item1) return BadRequest(new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 });
                var model = JsonConvert.DeserializeObject<TokenizationIdViewModel>(decryptedString.Item2);

                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int id = int.Parse(userId);
                var device = GetDevice();

                var changePrimaryCardResponse = await _cardService.ChangePrimaryCard(model.Id, id,device,ipAddress);
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
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public async Task<IActionResult> DeleteCard(EncryptedModel encryptedModel)
        {
            if (ModelState.IsValid)
            {
                var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
                if (!decryptedString.Item1) return BadRequest(new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 });
                var model = JsonConvert.DeserializeObject<TokenizationIdViewModel>(decryptedString.Item2);

                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int id = int.Parse(userId);
                var device = GetDevice();

                var deleteCardResponse = await _cardService.DeleteCard(model.Id, id,device,ipAddress);
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
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("[action]")]
        public async Task<IActionResult> CancelSubscription(EncryptedModel encryptedModel)
        {
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 });
            var model = JsonConvert.DeserializeObject<CancelSubscriptionViewModel>(decryptedString.Item2);

            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int id = int.Parse(userId);

            var cancelSubscriptionResult = await _restrictionService.CancelSubscription(id, model.Reason);
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
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public async Task<IActionResult> DeactivteReferee(EncryptedModel encryptedModel)
        {
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 });
            var model = JsonConvert.DeserializeObject<TokenizationIdViewModel>(decryptedString.Item2);

            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int id = int.Parse(userId);
            var response = await _restrictionService.DeactivateReferee(id, model.Id);
            if (response.Status)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        /// <summary>
        /// Activate referee
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public async Task<IActionResult> ActivateRefereeWithPrimaryCard(EncryptedModel encryptedModel)
        {
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 });
            var model = JsonConvert.DeserializeObject<TokenizationIdViewModel>(decryptedString.Item2);

            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int id = int.Parse(userId);
            var response = await _restrictionService.ActivateReferee(id, model.Id);
            if (response.Status)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        /// <summary>
        /// Action to Deactivate beneficiairies  under a coporate organisation.
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public async Task<IActionResult> DeactivateCompanyBeneficiary(EncryptedModel encryptedModel)
        {
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 });
            var beneficiaryListViewModel = JsonConvert.DeserializeObject<BeneficiaryListViewModel>(decryptedString.Item2);

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
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public async Task<IActionResult> DeactivateFamilyMember(EncryptedModel encryptedModel)
        {
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 });
            var model = JsonConvert.DeserializeObject<TokenizationIdViewModel>(decryptedString.Item2);

            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int id = int.Parse(userId);

            var familyProfile = await _repoWrapper.FamilyProfile.GetExtendedFamilyDetails(id);
            var insuranceProfile = familyProfile.InsuranceUserProfiles.Where(x => x.Id == model.Id).FirstOrDefault();
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
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public async Task<IActionResult> ActivateFamilyMember(EncryptedModel encryptedModel)
        {
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 });
            var model = JsonConvert.DeserializeObject<TokenizationIdViewModel>(decryptedString.Item2);

            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int id = int.Parse(userId);

            var response = await _restrictionService.ActivateFamilyMember(id, model.Id);
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

        private string GetDevice()
        {
            var userAgent = agent;
            string uaString = Convert.ToString(userAgent[0]);
            var uaParser = Parser.GetDefault();
            ClientInfo c = uaParser.Parse(uaString);
            return c.UA.ToString();
        }
    }
}
