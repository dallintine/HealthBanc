using AutoMapper;
using HealthBanc.DataAccess.Implementation;
using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Domain.Models;
using HealthBanc.Request.Tokenize;
using HealthBanc.Response;
using HealthBanc.Services;
using HealthBanc.Services.Tokenization;
using HealthBanc.ViewModels;
using HealthBanc.ViewModels.Tokenization;
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
        private readonly ICardRepository _cardRepository;

        public TokenizationController(TokenizationService tokenizationService,IMapper mapper, IAxaMansardUserProfileRepository mansardUserProfileRepository,
            IAxaMansardCompletionRepository completionRepository,SendLogViaWhatApp logViaWhatApp,ILogger<TokenizationController> logger,
            ITokenizationReferenceRepository tokenizationReference,ICardRepository cardRepository)
        {
            _tokenizationService = tokenizationService;
            _mapper = mapper;
            _mansardUserProfileRepository = mansardUserProfileRepository;
            _completionRepository = completionRepository;
            _logViaWhatApp = logViaWhatApp;
            _logger = logger;
            _tokenizationReference = tokenizationReference;
            _cardRepository = cardRepository;
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
                    var checkprofileComplete = await _completionRepository.GetCompletionStateBySuperAdminId(Id);
                    if(checkprofileComplete != null)
                    {
                        if (checkprofileComplete.TokenizationCompleted == true)
                        {
                            return BadRequest(new ResponseMessage { Message = "This card has been tokenised previously" });
                        }
                    }
                    else
                    {
                        return BadRequest(new ResponseMessage { Message = "Please create a user profile" });
                    }                    
                    
                    if (userAxamansardProfile != null && checkprofileComplete.ProfileCompleted == true)
                    {
                        var card = new ChargeCard();
                        var chargeCardRequest = _mapper.Map<Request.Tokenize.Card>(chargeCard.card);
                        card.email = userAxamansardProfile.Email; card.amount = userAxamansardProfile.Premium.ToString();
                        card.reference = Guid.NewGuid().ToString(); card.pin = chargeCard.pin;card.card = chargeCardRequest;
                        var cardResponse = await _tokenizationService.ChargeCard(card, Id);
                        if (cardResponse.Status == true && cardResponse.ResponseCode == 0)
                        { 
                            var tokenizeReference = new TokenizationReference();
                            tokenizeReference.SuperAdminId = Id; tokenizeReference.TokenReference = card.reference; tokenizeReference.Authorization_Code = cardResponse.AuthorizationCode;
                            _tokenizationReference.Create(tokenizeReference);
                            await _tokenizationReference.Save();

                            //If card count is 0. it means there is no card available, so the card tokenised will
                            //be the primary card so primary card status is set to 1 
                            //to set a primary card, card status is 0;
                            var cardStatus = userAxamansardProfile.Cards.Count == 0 ? 1 : 0;
                            var cardNum = chargeCard.card.number.Replace(" ", "");
                            var cardLastFourDigit = cardNum.Substring(cardNum.Length - 4);

                            var debitCard = new Domain.Models.DebitCard(Id, userAxamansardProfile.Id,cardStatus, cardLastFourDigit, tokenizeReference.Id);
                            _cardRepository.Create(debitCard);
                            await _cardRepository.Save();
                            
                            if(checkprofileComplete != null)
                            {
                                checkprofileComplete.TokenizationCompleted = true;
                                _completionRepository.Update(checkprofileComplete);
                                await _completionRepository.Save();
                            }
                            return Ok(cardResponse);
                        }
                        if(cardResponse.Status == true && cardResponse.ResponseCode == 12)
                        {
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
                _logViaWhatApp.SendLog("An error occurred while trying to charge card:" + ex.ToString());
                _logger.LogCritical("An error occurred while trying to submit user otp:" + ex);
                return BadRequest("An error occurred while trying to charge card:");                
            }
        }

        [Authorize]
        [HttpPost("[action]")]
        public async Task<IActionResult> SubmitOtp(SetOtpViewModel otpViewModel)
        {
            try
            {
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int Id = int.Parse(userId);

                var userAxamansardProfile = await _mansardUserProfileRepository.GetByAdminIdAsync(Id);
                var checkprofileComplete = await _completionRepository.GetCompletionStateBySuperAdminId(Id);
                if (userAxamansardProfile != null && checkprofileComplete.ProfileCompleted == true)
                {
                    var response = await _tokenizationService.SendOtp(otpViewModel.otp, userAxamansardProfile, otpViewModel.pin, otpViewModel.reference);
                    if (response.Status)
                    {
                        var tokenizeReference = new TokenizationReference();
                        tokenizeReference.SuperAdminId = Id; tokenizeReference.TokenReference = otpViewModel.reference; tokenizeReference.Authorization_Code = response.AuthorizationCode;
                        _tokenizationReference.Create(tokenizeReference);
                        await _tokenizationReference.Save();

                        if (checkprofileComplete != null)
                        {
                            checkprofileComplete.TokenizationCompleted = true;
                            _completionRepository.Update(checkprofileComplete);
                            await _completionRepository.Save();
                        }

                        return Ok(response);
                    } 
                    return BadRequest(response);
                }
                return BadRequest(new ResponseMessage { Message = "User has not been profiled", Status = false });               
            }
            catch(Exception ex)
            {
                _logViaWhatApp.SendLog("An error occurred while trying to submit user otp: " +ex.ToString());
                _logger.LogCritical("An error occurred while trying to submit user otp: " + ex);
                return BadRequest("An error occurred while trying to charge card: ");
            }
        }

        [Authorize]
        [HttpGet("[action]")]
        public async Task<IActionResult> InsertSubscription()
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            var userAxamansardProfile = await _mansardUserProfileRepository.GetByAdminIdAsync(Id);
            var tokenization = await _cardRepository.GetPrimaryCard(Id);

            if(userAxamansardProfile != null && tokenization != null)
            {
                var subscribe = _mapper.Map<SubscribePayment>(userAxamansardProfile);
                subscribe.Token = tokenization.Authorization_Code; subscribe.RequestId = tokenization.TokenReference;
                var fee = subscribe.RepaymentAmount == 1000 ? 20 : 50;
                subscribe.Fees = fee;

                var response = await _tokenizationService.InsertSubscription(subscribe);
                if (response.Status)
                {
                    return Ok(response);
                }
                return BadRequest(response);
            }
            _logger.LogCritical("An error occurrred. user wants to subscribe with no profile or tokenozation of cards");
            return BadRequest(new ResponseMessage { Message = "User cant subscribe unless card has been tokenize and user has a profile" });
        }

        [Authorize]
        [HttpGet("[action]")]
        public async Task<IActionResult> CancelSubscription()
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);
            return Ok(new ResponseMessage { Message="Subscription was cancelled successfully"});
        }

        /// <summary>
        /// Get subscriber cards
        /// </summary>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<List<Domain.Models.DebitCard>>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage<List<Domain.Models.DebitCard>>))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage<List<Domain.Models.DebitCard>>))]
        [Authorize]
        [HttpGet("[action]")]
        public async Task<IActionResult> GetCards()
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            var cards = await _mansardUserProfileRepository.GetByAdminIdAsync(Id);
            if(cards != null)
            {
                if(cards.Cards.Count > 0)
                {
                    return Ok(new ResponseMessage<List<Domain.Models.DebitCard>> { Data = cards.Cards, Status = true, Message = "Cards was fetchd successfully" });
                }
                return Ok(new ResponseMessage<List<Domain.Models.DebitCard>> { Message = "User has no card, Kindly add a card", Status = true });
            }
            return NotFound(new ResponseMessage { Status = false, Message = "User was not found" });
        }
    }
}
