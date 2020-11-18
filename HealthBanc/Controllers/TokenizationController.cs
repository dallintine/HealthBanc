using AutoMapper;
using Hangfire;
using HealthBanc.DataAccess.Implementation;
using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Domain.Models;
using HealthBanc.DTO;
using HealthBanc.DTO.ApplicationUserDTOs;
using HealthBanc.DTO.TokenizationDTO;
using HealthBanc.Helpers;
using HealthBanc.Request.Tokenize;
using HealthBanc.Response;
using HealthBanc.Services;
using HealthBanc.Services.AuditAndReport.AuditLog;
using HealthBanc.Services.InsuredCancelLiveSheet;
using HealthBanc.Services.Tokenization;
using HealthBanc.ViewModels;
using HealthBanc.ViewModels.Tokenization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
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
        private readonly IHttpContextAccessor _accessor;
        public string IpAddress;
        public StringValues agent;
        private SubscriptionDuration subDurationOptions { get; }
        private Paystack _paystackOptions { get; }
        private SendGridTemplateId _templateId { get; }


        public TokenizationController(TokenizationService tokenizationService,IMapper mapper, IAxaMansardUserProfileRepository mansardUserProfileRepository,
            IAxaMansardCompletionRepository completionRepository,SendLogViaWhatApp logViaWhatApp,ILogger<TokenizationController> logger,
            ITokenizationReferenceRepository tokenizationReference,ICardRepository cardRepository, LiveExcelList liveExcelList,IPaymentReferenceRepository paymentReference,
            AuditLogService auditLogServices, IHttpContextAccessor accessor,IOptions<SubscriptionDuration> subDuration,IOptions<Paystack> paystackOptions,
            IOptions<SendGridTemplateId> templateId)
        {
            _templateId = templateId.Value;
            _paystackOptions = paystackOptions.Value;
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
            _accessor = accessor;
            subDurationOptions = subDuration.Value;
            IpAddress = accessor.HttpContext.Connection.RemoteIpAddress.ToString();
            agent = accessor.HttpContext.Request.Headers["User-Agent"];
        }

        /// <summary>
        /// Charge user card for tokenization process
        /// </summary>
        /// <param name="chargeCard"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("[action]")]
        public async Task<IActionResult> ChargeCard(ChargeCardViewModel chargeCard)
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

                        var device = _auditLogServices.GetDevice(agent);

                        var card = new ChargeCard();
                        var chargeCardRequest = _mapper.Map<Request.Tokenize.Card>(chargeCard.card);
                        card.email = userAxamansardProfile.Email; card.amount = "100";
                        card.reference = Guid.NewGuid().ToString(); card.pin = chargeCard.pin; card.card = chargeCardRequest;
                        var cardResponse = await _tokenizationService.ChargeCard(card, Id,IpAddress,device);

                        var auditViewModel = new AuditLogViewModel(Id, null, null, "Attempted card tokenization", null);                       
                        BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel,IpAddress,device));

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
                            BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel2,IpAddress,device));

                            checkprofileComplete.TokenizationCompleted = true;
                            _completionRepository.Update(checkprofileComplete);    
                             
                            // Check if user does not have a subscrption status
                            if (userAxamansardProfile.SubscriptionStatus == null )
                            {
                                // Set subscrption status to true and update database
                                userAxamansardProfile.SubscriptionStatus = true;
                                _mansardUserProfileRepository.Update(userAxamansardProfile);                                   

                                // Create Audit thats user subscrption changed and run in background process
                                var auditViewModel3 = new AuditLogViewModel(Id, null, "Inactive subscription status", "Subscription Status Changed", "Active subscr" +
                                    "iption status, free one month trail");
                                BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel3,IpAddress,device));

                                // Get a Axamansard profile DTO to use in Background process.
                                var use = _mapper.Map<AxaMansardBackgroundDTO>(userAxamansardProfile);

                                // Check if Users are allowed to have a free trial.
                                if(subDurationOptions.FreeTrial == true)
                                {
                                    // Write user details to Goggle sheet by connecting to live google sheet API and also write to if users had a free trial
                                    var activatedUsersSheet = _mapper.Map<ActivatedUsersSheetDTO>(userAxamansardProfile);
                                    activatedUsersSheet.FreeTrail = true;
                                    await _liveExcelList.ActivatedUsers(activatedUsersSheet);

                                    if (subDurationOptions.FreeTrialDay)
                                    {
                                        BackgroundJob.Schedule(() => _tokenizationService.InsertSubscription(use,
                                            tokenizeReference, IpAddress, device,_paystackOptions.Channel,_paystackOptions.TokenType, _templateId.HealthInsured_PaymentReminder)
                                        , DateTime.Now.AddDays(subDurationOptions.FreeTrialDayDuration));
                                    }
                                    if (subDurationOptions.FreeTrialMinute)
                                    {
                                        var jobId = BackgroundJob.Schedule(() => _tokenizationService.InsertSubscription(use,
                                            tokenizeReference, IpAddress, device, _paystackOptions.Channel, _paystackOptions.TokenType, _templateId.HealthInsured_PaymentReminder)
                                        , DateTime.Now.AddMinutes(subDurationOptions.FreeTrialMinuteDuration));
                                    }
                                    if (subDurationOptions.FreeTrialMonth)
                                    {
                                        var jobId = BackgroundJob.Schedule(() => _tokenizationService.InsertSubscription(use,
                                            tokenizeReference, IpAddress, device, _paystackOptions.Channel, _paystackOptions.TokenType, _templateId.HealthInsured_PaymentReminder)
                                        , DateTime.Now.AddDays(subDurationOptions.FreeTrialMonthDuration));
                                    }
                                }
                                //If Users are not allowed to have a free trial run this
                                else
                                {
                                    // Write user details to Goggle sheet by connecting to live google sheet API and also write to if users do not a free trial
                                    var activatedUsersSheet = _mapper.Map<ActivatedUsersSheetDTO>(userAxamansardProfile);
                                    activatedUsersSheet.FreeTrail = false;
                                    await _liveExcelList.ActivatedUsers(activatedUsersSheet);

                                    if (subDurationOptions.Now)
                                    {
                                        BackgroundJob.Enqueue(() => _tokenizationService.InsertSubscription(use,
                                            tokenizeReference, IpAddress, device, _paystackOptions.Channel, _paystackOptions.TokenType, _templateId.HealthInsured_PaymentReminder));
                                    }
                                    if (subDurationOptions.Minutes)
                                    {
                                        var jobId = BackgroundJob.Schedule(() => _tokenizationService.InsertSubscription(use,
                                            tokenizeReference, IpAddress, device, _paystackOptions.Channel, _paystackOptions.TokenType, _templateId.HealthInsured_PaymentReminder)
                                        , DateTime.Now.AddMinutes(subDurationOptions.MinuteDuration));
                                    }
                                    if (subDurationOptions.Days)
                                    {
                                        var jobId = BackgroundJob.Schedule(() => _tokenizationService.InsertSubscription(use,
                                            tokenizeReference, IpAddress, device, _paystackOptions.Channel, _paystackOptions.TokenType, _templateId.HealthInsured_PaymentReminder)
                                        , DateTime.Now.AddDays(subDurationOptions.DaysDuration));
                                    }
                                }
                            }
                            await _mansardUserProfileRepository.Save();
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

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("[action]")]
        public async Task<IActionResult> SubmitOtp(SetOtpViewModel otpViewModel)
        {
            if (ModelState.IsValid)
            {
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int Id = int.Parse(userId);

                var userAxamansardProfile = await _mansardUserProfileRepository.GetByAdminIdAsync(Id);
                var checkprofileComplete = await _completionRepository.GetCompletionStateBySuperAdminId(Id);
                if (checkprofileComplete != null)
                {
                    if (userAxamansardProfile != null && checkprofileComplete.ProfileCompleted == true)
                    {
                        var device = _auditLogServices.GetDevice(agent);
                        var response = await _tokenizationService.SendOtp(otpViewModel.otp, userAxamansardProfile, otpViewModel.pin, otpViewModel.reference,IpAddress,device);
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

                            var auditViewModel2 = new AuditLogViewModel(Id, null, null, "Debit Card Added", null);
                            BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel2,IpAddress,device));


                            checkprofileComplete.TokenizationCompleted = true;
                            _completionRepository.Update(checkprofileComplete);                                         

                            if (userAxamansardProfile.SubscriptionStatus == null)
                            {
                                userAxamansardProfile.SubscriptionStatus = true;
                                _mansardUserProfileRepository.Update(userAxamansardProfile);

                                var auditViewModel3 = new AuditLogViewModel(Id, null, "Inactive subscription status", "Subscription Status Changed", "Active subscr" +
                                    "iption status, free one month trail");
                                BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel3,IpAddress,device));

                                var use = _mapper.Map<AxaMansardBackgroundDTO>(userAxamansardProfile);

                                if (subDurationOptions.FreeTrial == true)
                                {
                                    // Write user details to Goggle sheet by connecting to live google sheet API and also write to if users had a free trial
                                    var activatedUsersSheet = _mapper.Map<ActivatedUsersSheetDTO>(userAxamansardProfile);
                                    activatedUsersSheet.FreeTrail = true;
                                    await _liveExcelList.ActivatedUsers(activatedUsersSheet);

                                    if (subDurationOptions.FreeTrialDay)
                                    {
                                        BackgroundJob.Schedule(() => _tokenizationService.InsertSubscription(use,
                                            tokenizeReference, IpAddress, device, _paystackOptions.Channel, _paystackOptions.TokenType, _templateId.HealthInsured_PaymentReminder)
                                        , DateTime.Now.AddDays(subDurationOptions.FreeTrialDayDuration));
                                    }
                                    if (subDurationOptions.FreeTrialMinute)
                                    {
                                        BackgroundJob.Schedule(() => _tokenizationService.InsertSubscription(use,
                                            tokenizeReference, IpAddress, device, _paystackOptions.Channel, _paystackOptions.TokenType, _templateId.HealthInsured_PaymentReminder)
                                        , DateTime.Now.AddMinutes(subDurationOptions.FreeTrialMinuteDuration));
                                    }
                                    if (subDurationOptions.FreeTrialMonth)
                                    {
                                        BackgroundJob.Schedule(() => _tokenizationService.InsertSubscription(use,
                                            tokenizeReference, IpAddress, device, _paystackOptions.Channel, _paystackOptions.TokenType, _templateId.HealthInsured_PaymentReminder)
                                        , DateTime.Now.AddDays(subDurationOptions.FreeTrialMonthDuration));
                                    }

                                }
                                else
                                {
                                    // Write user details to Goggle sheet by connecting to live google sheet API and also write to if users do not a free trial
                                    var activatedUsersSheet = _mapper.Map<ActivatedUsersSheetDTO>(userAxamansardProfile);
                                    activatedUsersSheet.FreeTrail = false;
                                    await _liveExcelList.ActivatedUsers(activatedUsersSheet);

                                    if (subDurationOptions.Now)
                                    {
                                        BackgroundJob.Enqueue(() => _tokenizationService.InsertSubscription(use,
                                            tokenizeReference, IpAddress, device, _paystackOptions.Channel, _paystackOptions.TokenType, _templateId.HealthInsured_PaymentReminder));
                                    }
                                    if (subDurationOptions.Minutes)
                                    {
                                        var jobId = BackgroundJob.Schedule(() => _tokenizationService.InsertSubscription(use,
                                            tokenizeReference, IpAddress, device, _paystackOptions.Channel, _paystackOptions.TokenType, _templateId.HealthInsured_PaymentReminder)
                                        , DateTime.Now.AddMinutes(subDurationOptions.MinuteDuration));
                                    }
                                    if (subDurationOptions.Days)
                                    {
                                        var jobId = BackgroundJob.Schedule(() => _tokenizationService.InsertSubscription(use,
                                            tokenizeReference, IpAddress, device, _paystackOptions.Channel, _paystackOptions.TokenType, _templateId.HealthInsured_PaymentReminder)
                                        , DateTime.Now.AddDays(subDurationOptions.DaysDuration));
                                    }
                                }
                            }
                            await _mansardUserProfileRepository.Save();

                            return Ok(new ResponseMessage { Data = response.Data, Message = response.Message, Status = response.Status, ResponseCode = response.ResponseCode });
                        }
                        return BadRequest(new ResponseMessage { Data = response.Data, Message = response.Message, Status = response.Status, ResponseCode = response.ResponseCode });
                    }
                    return BadRequest(new ResponseMessage { Message = "User has not been profiled", Status = false });
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

        [Authorize(Roles = "SuperAdmin")]
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
            var device = _auditLogServices.GetDevice(agent);
            var cancelationResult = await _tokenizationService.CancelSubscription(axamansardProfile,IpAddress,device, _paystackOptions.Channel, _paystackOptions.TokenType);
            if(cancelationResult.Status == true)
            {
                var deactivatedUserSheet = _mapper.Map<InactiveUsersDTO>(axamansardProfile);
                BackgroundJob.Enqueue(() => _liveExcelList.RemoveFromActiveListToInactiveList(deactivatedUserSheet));

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
        [Authorize(Roles = "SuperAdmin")]
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

        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("[action]")]
        public async Task<IActionResult> ChangePrimaryCard(int cardId)
        {
            if (ModelState.IsValid)
            {
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int Id = int.Parse(userId);
                var presentPrimaryCard = await _cardRepository.GetPrimaryCard(Id);

                var device = _auditLogServices.GetDevice(agent);
                if (presentPrimaryCard == null)
                {
                    var auditViewModel3 = new AuditLogViewModel(Id, null, $"Primary card ID is {presentPrimaryCard.Id}", "Change Primary Card", $" Event Failed," +
                        $" Error: You dont have a card, Kindly tokenize a card");
                    BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel3,IpAddress,device));

                    return BadRequest(new ResponseMessage { Message = "You dont have a card, Kindly tokenize a card" });
                }
                var newPrimaryCard = await _cardRepository.GetCardByIdAsync(cardId, Id);
                if (newPrimaryCard == null) 
                {
                    var auditViewModel3 = new AuditLogViewModel(Id, null, $"Primary card ID is {presentPrimaryCard.Id}", "Change Primary Card", $" Event Failed," +
                        $" Error: No secondary card tied to you was found");
                    BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel3,IpAddress,device));
                    return NotFound(new ResponseMessage { Message = "No secondary card tied to you was found" });
                }
                if (newPrimaryCard.Status == 1) {

                    var auditViewModel3 = new AuditLogViewModel(Id, null, $"Primary card ID is {presentPrimaryCard.Id}", "Change Primary Card", $" Event Failed," +
                       $" Error: This card is presently the primary card");
                    BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel3, IpAddress, device));
                    return BadRequest(new ResponseMessage { Message = "This card is presently the primary card" });
                }
                if (presentPrimaryCard != null && newPrimaryCard != null)
                {
                    var userAxamansardProfile = await _mansardUserProfileRepository.UserAndPaymentReference(Id);
                    var axaMansardBackgroundDTO = _mapper.Map<AxaMansardBackgroundDTO>(userAxamansardProfile);

                    var activePaymentReference = userAxamansardProfile.PaymentReferences.FirstOrDefault(x => x.Active == true);

                    BackgroundJob.Enqueue(() => _tokenizationService.UpdateSubscription(axaMansardBackgroundDTO, newPrimaryCard.TokenizationReference.Authorization_Code,
                        activePaymentReference.RequestId, IpAddress, device, _paystackOptions.Channel, _paystackOptions.TokenType));

                    presentPrimaryCard.Status = 0;
                    _cardRepository.Update(presentPrimaryCard);
                    newPrimaryCard.Status = 1;
                    _cardRepository.Update(newPrimaryCard);
                    await _cardRepository.Save();

                    var auditViewModel3 = new AuditLogViewModel(Id, null, $"Primary card ID is {presentPrimaryCard.Id}", "Change Primary Card", $"New primary card ID is {cardId}");
                    BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel3, IpAddress, device));                  


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

        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("[action]")]
        public async Task<IActionResult> DeleteCard(int cardId)
        {
            if (ModelState.IsValid)
            {
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int Id = int.Parse(userId);
                var card = await _cardRepository.GetCardByIdAsync(cardId, Id);
                var userAxamansardProfile = await _mansardUserProfileRepository.GetByAdminIdAsync(Id);

                if (card == null) return NotFound(new ResponseMessage { Message = "Card  was not found" });
                if (card.Status == 1 && userAxamansardProfile.SubscriptionStatus == true) return BadRequest(new ResponseMessage { Message = "Kindly add a new card and change primary card to delete present primary card" });
                if(card.TokenizationReference != null)
                {
                    _tokenizationReference.Delete(card.TokenizationReference);
                }
                _cardRepository.Delete(card);
                await _cardRepository.Save();

                var auditViewModel3 = new AuditLogViewModel(Id, null, $"Card id is ${cardId}", "Delete Card", $"Card was deleted successfully");
                var device = _auditLogServices.GetDevice(agent);
                BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel3,IpAddress,device));
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

        [Authorize(Roles = "SuperAdmin")]
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
            var axaMansardBackgroundDTO = _mapper.Map<AxaMansardBackgroundDTO>(userAxamansardProfile);
            var tokenizationReference = await _cardRepository.GetPrimaryCardReference(Id);    
            
            if(tokenizationReference == null)
            {
                return BadRequest(new ResponseMessage {Message="Kindly add a primary card to reactivate subscription" });
            }

            var auditViewModel3 = new AuditLogViewModel(Id, null, $"Inactive subscription", "Reactivated subscription", $"Subscription was reactivated");
            var device = _auditLogServices.GetDevice(agent);
            BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel3,IpAddress,device));

            BackgroundJob.Enqueue(() => _tokenizationService.InsertSubscription(axaMansardBackgroundDTO, tokenizationReference,IpAddress,device, _paystackOptions.Channel
                , _paystackOptions.TokenType, _templateId.HealthInsured_PaymentReminder));

            var activatedSheet = _mapper.Map<ActivatedUsersSheetDTO>(userAxamansardProfile);
            BackgroundJob.Enqueue(() => _liveExcelList.RemoveFromInactiveListToActiveList(activatedSheet));

            userAxamansardProfile.SubscriptionStatus = true;
            _mansardUserProfileRepository.Update(userAxamansardProfile);

            await _mansardUserProfileRepository.Save();
            return Ok(new ResponseMessage {Message="Reactivation was successful",Status=true });
        }
    }

}
