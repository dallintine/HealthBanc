using AutoMapper;
using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Domain.Models;
using HealthBanc.Request.Tokenize;
using HealthBanc.Response;
using HealthBanc.Services;
using HealthBanc.Services.Tokenization;
using HealthBanc.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Twilio.Rest.Api.V2010.Account;

namespace HealthBanc.Controllers
{
    [Route("v1/api/[controller]")]
    [ApiController]
    public class TokenizationController : ControllerBase
    {
        private readonly TokenizationService _tokenizationService;
        private readonly IMapper _mapper;
        private readonly IAxaMansardUserProfileRepository _mansardUserProfileRepository;
        private readonly IAxaMansardCompletionRepository _completionRepository;
        private readonly SendLogViaWhatApp _logViaWhatApp;
        private readonly ILogger<TokenizationController> _logger;
        private readonly ITokenizationReferenceRepository _tokenizationReference;

        public TokenizationController(TokenizationService tokenizationService,IMapper mapper, IAxaMansardUserProfileRepository mansardUserProfileRepository,
            IAxaMansardCompletionRepository completionRepository,SendLogViaWhatApp logViaWhatApp,ILogger<TokenizationController> logger,
            ITokenizationReferenceRepository tokenizationReference)
        {
            _tokenizationService = tokenizationService;
            _mapper = mapper;
            _mansardUserProfileRepository = mansardUserProfileRepository;
            _completionRepository = completionRepository;
            _logViaWhatApp = logViaWhatApp;
            _logger = logger;
            _tokenizationReference = tokenizationReference;
        }

        /// <summary>
        /// Charge user card for tokenization process
        /// </summary>
        /// <param name="chargeCard"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize]
        [HttpPost("[action]")]
        public async Task<IActionResult> ChargeCard(ChargeCardViewModel chargeCard)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                    int Id = int.Parse(userId);

                    var userAxamansardProfile = await _mansardUserProfileRepository.GetByAdminIdAsync(Id);
                    if (userAxamansardProfile != null)
                    {
                        var card = new ChargeCard();
                        var chargeCardRequest = _mapper.Map<Request.Tokenize.Card>(chargeCard.card);
                        card.email = userAxamansardProfile.Email; card.amount = userAxamansardProfile.Premium.ToString();
                        card.reference = Guid.NewGuid().ToString(); card.pin = chargeCard.pin;card.card = chargeCardRequest;
                        var cardResponse = await _tokenizationService.ChargeCard(card, Id);
                        if (cardResponse.Status == true)
                        {
                            var tokenizeReference = new TokenizationReference();
                            tokenizeReference.SuperAdminId = Id; tokenizeReference.TokenReference = card.reference;
                            _tokenizationReference.Create(tokenizeReference);
                            await _tokenizationReference.Save();
                            return Ok(cardResponse);
                        }
                        return BadRequest(cardResponse);
                    }
                    return BadRequest(new ResponseMessage { Message = "User has not been profiled", Status = false });
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
                return BadRequest(new ResponseMessage { Data = errors, Message = "There were validation errors" });
            }
            catch(Exception ex)
            {
                _logViaWhatApp.SendLog("An error occurred while trying to charge card: " + ex.ToString());
                _logger.LogCritical("An error occurred while trying to submit user otp: " + ex);
                return BadRequest("An error occurred while trying to charge card: "+ex);                
            }
            
        }

        [Authorize]
        [HttpPost("[action]")]
        public async Task<IActionResult> SubmitOtp(string otp,string pin,string reference)
        {
            try
            {
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int Id = int.Parse(userId);

                var userAxamansardProfile = await _mansardUserProfileRepository.GetByAdminIdAsync(Id);
                if (userAxamansardProfile != null)
                {
                    var response = await _tokenizationService.SendOtp(otp, userAxamansardProfile, pin,reference);
                    if (response.Status) return Ok(response);
                    return BadRequest(response);
                }
                return BadRequest(new ResponseMessage { Message = "User has not been profiled", Status = false });               
            }
            catch(Exception ex)
            {
                _logViaWhatApp.SendLog("An error occurred while trying to submit user otp: " +ex.ToString());
                _logger.LogCritical("An error occurred while trying to submit user otp: " + ex);
                return BadRequest("An error occurred while trying to charge card: " + ex);
            }
        }
    }
}
