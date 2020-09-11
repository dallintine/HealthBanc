using AutoMapper;
using HealthBanc.DataAccess.Implementation;
using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Domain.Models;
using HealthBanc.DTO.TokenizationDTO;
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
                        if (userAxamansardProfile != null && checkprofileComplete.ProfileCompleted == true)
                        {
                            var card = new ChargeCard();
                            var chargeCardRequest = _mapper.Map<Request.Tokenize.Card>(chargeCard.card);
                            card.email = userAxamansardProfile.Email; card.amount = userAxamansardProfile.Premium.ToString();
                            card.reference = Guid.NewGuid().ToString(); card.pin = chargeCard.pin; card.card = chargeCardRequest;
                            var cardResponse = await _tokenizationService.ChargeCard(card, Id);
                            if (cardResponse.Status == true && cardResponse.ResponseCode == 0)
                            {
                                var checkIfCardWasPreviouslyTokenized = await _cardRepository.CheckIfCardWasPreviouslyTokenized(Id, cardResponse.Signature);
                                if(checkIfCardWasPreviouslyTokenized != null) return BadRequest(new ResponseMessage { Message="This card was previously tokenized successfully"});

                                var tokenizeReference = new TokenizationReference();
                                tokenizeReference.SuperAdminId = Id; tokenizeReference.TokenReference = card.reference; tokenizeReference.Authorization_Code = cardResponse.AuthorizationCode;
                                _tokenizationReference.Create(tokenizeReference);
                                await _tokenizationReference.Save();

                                //If card count is 0. it means there is no card available, so the card tokenised will
                                //be the primary card so primary card status is set to 1 
                                //to set a primary card, card status is 0;
                                var cardStatus = userAxamansardProfile.Cards.Count == 0 ? 1 : 0;

                                var debitCard = new Domain.Models.DebitCard(Id, userAxamansardProfile.Id, cardStatus, cardResponse.LastDigit, cardResponse.Signature, cardResponse.Type, tokenizeReference.Id);
                                _cardRepository.Create(debitCard);
                                //await _cardRepository.Save();

                                checkprofileComplete.TokenizationCompleted = true;
                                _completionRepository.Update(checkprofileComplete);
                                await _completionRepository.Save();

                                return Ok(new ResponseMessage { Status = cardResponse.Status, ResponseCode = cardResponse.ResponseCode, Message = cardResponse.Message });
                            }
                            if (cardResponse.Status == true && cardResponse.ResponseCode == 12)
                            {
                                return Ok(new ResponseMessage { Data = cardResponse.Data, Message = cardResponse.Message, Status = cardResponse.Status, ResponseCode = cardResponse.ResponseCode });
                            }
                            return BadRequest(new ResponseMessage { Data = cardResponse.Data, Message = cardResponse.Message, Status = cardResponse.Status, ResponseCode = cardResponse.ResponseCode });
                        }
                        return BadRequest(new ResponseMessage { Message = "User has not been profiled,kindly create your profile", Status = false });
                    }
                    return BadRequest(new ResponseMessage { Message = "User has not been profiled,kindly create your profile", Status = false });
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
            if (ModelState.IsValid)
            {
                try
                {
                    string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                    int Id = int.Parse(userId);

                    var userAxamansardProfile = await _mansardUserProfileRepository.GetByAdminIdAsync(Id);
                    var checkprofileComplete = await _completionRepository.GetCompletionStateBySuperAdminId(Id);
                    if (checkprofileComplete != null)
                    {
                        if (userAxamansardProfile != null && checkprofileComplete.ProfileCompleted == true)
                        {
                            var response = await _tokenizationService.SendOtp(otpViewModel.otp, userAxamansardProfile, otpViewModel.pin, otpViewModel.reference);
                            if (response.Status)
                            {
                                var checkIfCardWasPreviouslyTokenized = await _cardRepository.CheckIfCardWasPreviouslyTokenized(Id, response.Signature);
                                if (checkIfCardWasPreviouslyTokenized != null) return BadRequest(new ResponseMessage { Message = "This card was previously tokenized successfully" });

                                var tokenizeReference = new TokenizationReference();
                                tokenizeReference.SuperAdminId = Id; tokenizeReference.TokenReference = otpViewModel.reference; tokenizeReference.Authorization_Code = response.AuthorizationCode;
                                _tokenizationReference.Create(tokenizeReference);
                                await _tokenizationReference.Save();

                                //If card count is 0. it means there is no card available, so the card tokenised will
                                //be the primary card so primary card status is set to 1 
                                //to set a primary card, card status is 0;
                                var cardStatus = userAxamansardProfile.Cards.Count == 0 ? 1 : 0;

                                var debitCard = new Domain.Models.DebitCard(Id, userAxamansardProfile.Id, cardStatus, response.LastDigit, response.Signature, response.Type, tokenizeReference.Id);
                                _cardRepository.Create(debitCard);
                                await _cardRepository.Save();

                                checkprofileComplete.TokenizationCompleted = true;
                                _completionRepository.Update(checkprofileComplete);
                                await _completionRepository.Save();

                                return Ok(new ResponseMessage { Data = response.Data, Message = response.Message, Status = response.Status, ResponseCode = response.ResponseCode });
                            }
                            return BadRequest(new ResponseMessage { Data = response.Data, Message = response.Message, Status = response.Status, ResponseCode = response.ResponseCode });
                        }
                        return BadRequest(new ResponseMessage { Message = "User has not been profiled", Status = false });
                    }
                    return BadRequest(new ResponseMessage { Message = "User has not been profiled,kindly create your profile", Status = false });
                }
                catch (Exception ex)
                {
                    _logViaWhatApp.SendLog("An error occurred while trying to submit user otp: " + ex.ToString());
                    _logger.LogCritical("An error occurred while trying to submit user otp: " + ex);
                    return BadRequest("An error occurred while trying to charge card: ");
                }
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

        [Authorize]
        [HttpGet("[action]")]
        public async Task<IActionResult> InsertSubscription()
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            var userAxamansardProfile = await _mansardUserProfileRepository.GetByAdminIdAsync(Id);
            var tokenization = await _cardRepository.GetPrimaryCardReference(Id);

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
        [ProducesResponseType(200, Type = typeof(ResponseMessage<List<CardDTO>>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage<List<CardDTO>>))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage<List<CardDTO>>))]
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
                    var cardDTO = _mapper.Map<List<DebitCard>, List<CardDTO>>(cards.Cards);
                    return Ok(new ResponseMessage<List<CardDTO>> { Data = cardDTO, Status = true, Message = "Cards was fetchd successfully" });
                }
                return BadRequest(new ResponseMessage<List<CardDTO>> { Message = "User has no card, Kindly add a card", Status = true });
            }
            return NotFound(new ResponseMessage<List<DebitCard>> { Status = false, Message = "User was not found" });
        }

        [Authorize]
        [HttpGet("[action]")]
        public async Task<IActionResult> ChangePrimaryCard(int cardId)
        {
            if (ModelState.IsValid)
            {
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int Id = int.Parse(userId);
                var presentPrimaryCard = await _cardRepository.GetPrimaryCard(Id);
                if (presentPrimaryCard == null) return BadRequest(new ResponseMessage { Message = "You dont have a card, Kindly tokenize a card" });
                var newPrimaryCard = await _cardRepository.GetCardByIdAsync(cardId, Id);
                if (newPrimaryCard == null) return NotFound(new ResponseMessage { Message = "No secondary card tied to you was found" });
                if (presentPrimaryCard != null && newPrimaryCard != null)
                {
                    presentPrimaryCard.Status = 0;
                    _cardRepository.Update(presentPrimaryCard);
                    newPrimaryCard.Status = 1;
                    _cardRepository.Update(newPrimaryCard);
                    await _cardRepository.Save();
                    return Ok(new ResponseMessage { Message = "Primary card was changed successfully" });
                }
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
            return BadRequest(new ResponseMessage { Data = errors, Message = errors.FirstOrDefault().ToString()});
        }

        [Authorize]
        [HttpGet("[action]")]
        public async Task<IActionResult> DeleteCard(int cardId)
        {
            if (ModelState.IsValid)
            {
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int Id = int.Parse(userId);
                var card = await _cardRepository.GetCardByIdAsync(cardId, Id);
                var tokenizationReference = await _tokenizationReference.GetBySuperAdminIdAsync(Id);
                var userCardCount = await _cardRepository.GetUserCardCount(Id);
                if (card == null) return NotFound(new ResponseMessage { Message = "No card tied to you was found" });
                if (card.Status == 1 && userCardCount > 1) return BadRequest(new ResponseMessage { Message = "Set new primary card before you delete this card" });
                if(tokenizationReference != null)
                {
                    _tokenizationReference.Delete(tokenizationReference);
                }
                _cardRepository.Delete(card);
                await _cardRepository.Save();
                return Ok(new ResponseMessage { Message = "Card was deleted successfully" });
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
    }
}
