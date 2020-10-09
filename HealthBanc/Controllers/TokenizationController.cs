using AutoMapper;
using Hangfire;
using HealthBanc.DataAccess.Implementation;
using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Domain.Models;
using HealthBanc.DTO;
using HealthBanc.DTO.ApplicationUserDTOs;
using HealthBanc.DTO.TokenizationDTO;
using HealthBanc.Request.Tokenize;
using HealthBanc.Response;
using HealthBanc.Services;
using HealthBanc.Services.AuditAndReport.AuditLog;
using HealthBanc.Services.InsuredCancelLiveSheet;
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
        private readonly LiveExcelList _liveExcelList;
        private readonly IPaymentReferenceRepository _paymentReference;
        private readonly AuditLogService _auditLogServices;

        public TokenizationController(TokenizationService tokenizationService,IMapper mapper, IAxaMansardUserProfileRepository mansardUserProfileRepository,
            IAxaMansardCompletionRepository completionRepository,SendLogViaWhatApp logViaWhatApp,ILogger<TokenizationController> logger,
            ITokenizationReferenceRepository tokenizationReference,ICardRepository cardRepository, LiveExcelList liveExcelList,IPaymentReferenceRepository paymentReference,
            AuditLogService auditLogServices)
        {
            _tokenizationService = tokenizationService;
            _mapper = mapper;
            _mansardUserProfileRepository = mansardUserProfileRepository;
            _completionRepository = completionRepository;
            _logViaWhatApp = logViaWhatApp;
            _logger = logger;
            _tokenizationReference = tokenizationReference;
            _cardRepository = cardRepository;
            _liveExcelList = liveExcelList;
            _paymentReference = paymentReference;
            _auditLogServices = auditLogServices;
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
                            var cardNumber = chargeCard.card.number.Replace(" ", "");
                            var checkIfCardWasPreviouslyTokenized = await _cardRepository.CheckIfCardWasPreviouslyTokenized(Id, cardNumber.Substring(cardNumber.Length - 4));
                            if (checkIfCardWasPreviouslyTokenized != null) return BadRequest(new ResponseMessage { Message = "This card was previously tokenized successfully" });

                            var card = new ChargeCard();
                            var chargeCardRequest = _mapper.Map<Request.Tokenize.Card>(chargeCard.card);
                            card.email = userAxamansardProfile.Email; card.amount = userAxamansardProfile.Premium.ToString();
                            card.reference = Guid.NewGuid().ToString(); card.pin = chargeCard.pin; card.card = chargeCardRequest;
                            var cardResponse = await _tokenizationService.ChargeCard(card, Id);

                            var auditViewModel = new AuditLogViewModel(Id, null, null, "Attempted card tokenization", null);
                            BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel));

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

                                var debitCard = new Domain.Models.DebitCard(Id, userAxamansardProfile.Id, cardStatus, cardResponse.LastDigit, cardResponse.Type, tokenizeReference.Id);
                                _cardRepository.Create(debitCard);

                                var auditViewModel2 = new AuditLogViewModel(Id, null, null, "Debit Card Added", null);
                                BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel2));

                                checkprofileComplete.TokenizationCompleted = true;
                                _completionRepository.Update(checkprofileComplete);    
                                
                                if (userAxamansardProfile.SubscriptionStatus == null )
                                {
                                    userAxamansardProfile.SubscriptionStatus = true;
                                    _mansardUserProfileRepository.Update(userAxamansardProfile);
                                    await _mansardUserProfileRepository.Save();

                                    var auditViewModel3 = new AuditLogViewModel(Id, null, "Inactive subscription status", "Subscription Status Changed", "Active subscr" +
                                        "iption status, free one month trail");
                                    BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel3));

                                    var use = _mapper.Map<AxaMansardBackgroundDTO>(userAxamansardProfile);

                                    var jobId = BackgroundJob.Schedule(() => _tokenizationService.InsertSubscription(use,
                                       tokenizeReference), DateTime.Now.AddMinutes(2));
                                }
                                return Ok(new ResponseMessage {Data=cardResponse.Data, Status = cardResponse.Status, ResponseCode = cardResponse.ResponseCode, Message = cardResponse.Message });
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
                _logViaWhatApp.SendLog("An error occurred while trying to charge card:" + ex.Message.ToString());
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
                                var tokenizeReference = new TokenizationReference();
                                tokenizeReference.SuperAdminId = Id; tokenizeReference.TokenReference = otpViewModel.reference; tokenizeReference.Authorization_Code = response.AuthorizationCode;
                                _tokenizationReference.Create(tokenizeReference);
                                await _tokenizationReference.Save();

                                //If card count is 0. it means there is no card available, so the card tokenised will
                                //be the primary card so primary card status is set to 1 
                                //to set a primary card, card status is 0;
                                var cardStatus = userAxamansardProfile.Cards.Count == 0 ? 1 : 0;

                                var debitCard = new Domain.Models.DebitCard(Id, userAxamansardProfile.Id, cardStatus, response.LastDigit, response.Type, tokenizeReference.Id);
                                _cardRepository.Create(debitCard);
                                await _cardRepository.Save();

                                var auditViewModel2 = new AuditLogViewModel(Id, null, null, "Debit Card Added", null);
                                BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel2));


                                checkprofileComplete.TokenizationCompleted = true;
                                _completionRepository.Update(checkprofileComplete);
                                await _completionRepository.Save();                                                             

                                if (userAxamansardProfile.Cards.Count == 1)
                                {
                                    userAxamansardProfile.SubscriptionStatus = true;
                                    _mansardUserProfileRepository.Update(userAxamansardProfile);
                                    await _mansardUserProfileRepository.Save();

                                    var auditViewModel3 = new AuditLogViewModel(Id, null, "Inactive subscription status", "Subscription Status Changed", "Active subscr" +
                                        "iption status, free one month trail");
                                    BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel3));

                                    var use = _mapper.Map<AxaMansardBackgroundDTO>(userAxamansardProfile);

                                    var jobId = BackgroundJob.Schedule(() => _tokenizationService.InsertSubscription(use,
                                       tokenizeReference), DateTime.Now.AddMinutes(2));
                                }

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
        public async Task<IActionResult> CancelSubscription(string reason)
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);
            var axamansardProfile = await _mansardUserProfileRepository.GetByAdminIdAsync(Id);
            if (axamansardProfile.SubscriptionStatus == false)
            {
                return BadRequest(new ResponseMessage { Message = "You have no active subscription", Status = false });
            }
            var cancelationResult = await _tokenizationService.CancelSubscription(axamansardProfile);
            if(cancelationResult.Status == true)
            {
                var listDTO = _mapper.Map<InactiveUsersDTO>(axamansardProfile);
                await _liveExcelList.WriteAsync(listDTO);

                var profileDTO = _mapper.Map<AxaMansardUserDTO>(axamansardProfile);
                return Ok(new ResponseMessage { Data = profileDTO, Message = "Subscription was cancelled successfully", Status = true });
            }
            return BadRequest(cancelationResult);            
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
                var emptyCardDTO = new List<CardDTO>();
                return Ok(new ResponseMessage<List<CardDTO>> {Data = emptyCardDTO, Message = "User has no card, Kindly add a card", Status = true });
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
                if (presentPrimaryCard == null)
                {
                    var auditViewModel3 = new AuditLogViewModel(Id, null, $"Primary card ID is {presentPrimaryCard.Id}", "Change Primary Card", $" Event Failed," +
                        $" Error: You dont have a card, Kindly tokenize a card");
                    BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel3));

                    return BadRequest(new ResponseMessage { Message = "You dont have a card, Kindly tokenize a card" });
                }
                var newPrimaryCard = await _cardRepository.GetCardByIdAsync(cardId, Id);
                if (newPrimaryCard == null) 
                {
                    var auditViewModel3 = new AuditLogViewModel(Id, null, $"Primary card ID is {presentPrimaryCard.Id}", "Change Primary Card", $" Event Failed," +
                        $" Error: You dont have a card, Kindly tokenize a card");
                    BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel3));
                    return NotFound(new ResponseMessage { Message = "No secondary card tied to you was found" });
                }
                if (newPrimaryCard.Status == 1) {

                    var auditViewModel3 = new AuditLogViewModel(Id, null, $"Primary card ID is {presentPrimaryCard.Id}", "Change Primary Card", $" Event Failed," +
                       $" Error: This card is presently the primary card");
                    BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel3));
                    return BadRequest(new ResponseMessage { Message = "This card is presently the primary card" });
                }
                if (presentPrimaryCard != null && newPrimaryCard != null)
                {
                    presentPrimaryCard.Status = 0;
                    _cardRepository.Update(presentPrimaryCard);
                    newPrimaryCard.Status = 1;
                    _cardRepository.Update(newPrimaryCard);
                    await _cardRepository.Save();

                    var auditViewModel3 = new AuditLogViewModel(Id, null, $"Primary card ID is {presentPrimaryCard.Id}", "Change Primary Card", $"New primary card ID is {cardId}");
                    BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel3));

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
                var userAxamansardProfile = await _mansardUserProfileRepository.GetByAdminIdAsync(Id);
                //var userCardCount = await _cardRepository.GetUserCardCount(Id);
                if (card == null) return NotFound(new ResponseMessage { Message = "Card  was not found" });
                if (card.Status == 1 && userAxamansardProfile.SubscriptionStatus == true) return BadRequest(new ResponseMessage { Message = "Kindly add a new card and change primary card to delete present primary card" });
                if(card.TokenizationReference != null)
                {
                    _tokenizationReference.Delete(card.TokenizationReference);
                }
                _cardRepository.Delete(card);
                await _cardRepository.Save();

                var auditViewModel3 = new AuditLogViewModel(Id, null, $"Card id is ${cardId}", "Delete Card", $"Card was deleted successfully");
                BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel3));
                return Ok(new ResponseMessage { Message = "Card was deleted successfully",Status=true });
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

        [HttpGet("[action]")]
        public async Task<IActionResult> ReactivateWithPresentPrimaryCard()
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            var userAxamansardProfile = await _mansardUserProfileRepository.GetByAdminIdAsync(Id);
            if(userAxamansardProfile.SubscriptionStatus == true)
            {
                return BadRequest(new ResponseMessage { Message = "Subscription is currently active", Status=false });
            }
            var use = _mapper.Map<AxaMansardBackgroundDTO>(userAxamansardProfile);
            var tokenizationReference = await _cardRepository.GetPrimaryCardReference(Id);
            await _tokenizationService.InsertSubscription(use, tokenizationReference);
            var auditViewModel3 = new AuditLogViewModel(Id, null, $"Inactive subscription", "Reactivated subscription", $"Subscription was reactivated");
            BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel3));
            //if (result.Status == true)
            //{
            userAxamansardProfile.SubscriptionStatus = true;
            _mansardUserProfileRepository.Update(userAxamansardProfile);

            await _mansardUserProfileRepository.Save();
            return Ok(new ResponseMessage {Message="Reactivation was successful",Status=true });
            //} 
            //return BadRequest(result);
        }

        //[HttpPost("[action]")]
        //public async Task<IActionResult> RecurJobs()
        //{
        //    RecurringJob.AddOrUpdate(() => Console.WriteLine("Test succeded"), Cron.Minutely);
        //    return Ok("Intiated");

        //}
    }
}
