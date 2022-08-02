using Application.API_RequestModel.Paystack;
using Application.API_ResponseModel.Paystack;
using Application.DTO;
using Application.HealthInsured_AxaMansard_Service.Insurance;
using Application.Helpers;
using Application.Interfaces;
using Application.Services.HealthInsured;
using Application.Services.HealthInsured.Insurance;
using Application.Services.HealthInsured_AxaMansard.Insurance;
using Application.Services.Paystack;
using Application.ViewModels;
using Application.ViewModels.HealthInsured;
using Application.ViewModels.Paystack;
using AutoMapper;
using DataAccess;
using Domain.Enums;
using Domain.Models;
using Domain.Models.Axa.Hygeia_Insurance;
using Domain.Models.Axa_Hygeia_Insurance;
using Domain.Models.ReportAndLogs;
using Hangfire;
using HealthBanc.DTO.HealthInsured_AxaMansard;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Card
{
    public class Card_SubscriptionService
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly ILogger<Card_SubscriptionService> _logger;
        private readonly InsuranceService _insuranceService;
        private readonly IMapper _mapper;
        private readonly IUniqueIdentifier _uniqueIdentifier;
        private readonly TokenizationService _tokenizationService;
        private readonly PaystackService _paystackService;
        private readonly FamilyInsuranceService _familyInsurance;
        private readonly IEmailSender _emailSender;
        private readonly HMOIntegrationService _hmoIntegrationService;

        private SubscriptionDuration SubscriptionAccessor { get; }

        public Card_SubscriptionService(IRepositoryWrapper repositoryWrapper,ILogger<Card_SubscriptionService> logger,InsuranceService insuranceService,IMapper mapper,IOptions<SubscriptionDuration> subscriptionAccessor
            , IUniqueIdentifier uniqueIdentifier,TokenizationService tokenizationService,PaystackService paystackService, FamilyInsuranceService familyInsurance,
            IEmailSender emailSender, HMOIntegrationService hmoIntegrationService)
        {
            _repositoryWrapper = repositoryWrapper;
            _logger = logger;
            _insuranceService = insuranceService;
            _mapper = mapper;
            _uniqueIdentifier = uniqueIdentifier;
            _tokenizationService = tokenizationService;
            _paystackService = paystackService;
            _familyInsurance = familyInsurance;
            _emailSender = emailSender;
            _hmoIntegrationService = hmoIntegrationService;
            SubscriptionAccessor = subscriptionAccessor.Value;
        }

        /// <summary>
        /// Service to delete debit card
        /// </summary>
        /// <param name="cardId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> DeleteCard(int cardId, int userId)
        {
            _logger.LogInformation($"Processing Deleting Card [CardId :{cardId} | UserId : {userId}]\n");
            var card = await _repositoryWrapper.Card.GetCardByIdAsync(cardId, userId);
            bool isCardDeletable = false;
            if (card == null)
            {
                _logger.LogInformation($"Delete card was terminated [Reason : Card record was not found] \n");
                return new ResponseMessage { Message = "Card  was not found", ResponseCode = 12 };
            }

            //If card is for an insurance profile
            if (card.InsuranceUserProfileId != null)
            {
                _logger.LogInformation($"Delete card processing for individual insurance profile \n");
                var insuranceProfile = await _repositoryWrapper.InsuranceProfile.GetByUserIdAsync(userId);
                if ((card.Status == (int)DebitCard_StatusValue.primary && insuranceProfile.SubscriptionStatus != true) || card.Status == (int)DebitCard_StatusValue.secondary)
                {
                    _logger.LogInformation($"Delete card processing for individual insurance profile [ Card is deletable] \n");
                    isCardDeletable = true;
                    var activityLog = new ActivityLog(insuranceProfile.Id, null, null, "Debit Card Was Removed", ServiceNames.HealthInsured.ToString());
                    _repositoryWrapper.ActivityLog.Create(activityLog);
                }
            }
            else if (card.CompanyProfileId != null)
            {
                _logger.LogInformation($"Delete card processing for corporate insurance profile \n");
                var companyProfile = await _repositoryWrapper.CompanyProfile.GetCompanyProfileByUserId(userId);
                if (card.Status == (int)DebitCard_StatusValue.secondary)
                {
                    _logger.LogInformation($"Delete card processing for company insurance profile [ Card is deletable] \n");
                    isCardDeletable = true;
                    var activityLog = new ActivityLog(null, companyProfile.Id, null, "Debit Card Was Removed", ServiceNames.HealthInsured.ToString());
                    _repositoryWrapper.ActivityLog.Create(activityLog);
                }
            }
            else
            {
                _logger.LogInformation($"Delete card processing for family insurance profile \n");
                var familyProfile = await _repositoryWrapper.FamilyProfile.GetByUserId(userId);
                if (card.Status == (int)DebitCard_StatusValue.secondary)
                {
                    _logger.LogInformation($"Delete card processing for family insurance profile [ Card is deletable] \n");
                    isCardDeletable = true;
                    var activityLog = new ActivityLog(null, null, familyProfile.Id, "Debit Card Was Removed", ServiceNames.HealthInsured.ToString());
                    _repositoryWrapper.ActivityLog.Create(activityLog);
                }
            }

            if (isCardDeletable)
            {
                _repositoryWrapper.Card.Delete(card);
                await _repositoryWrapper.Save();
                _logger.LogInformation($"Card was deleted successfully \n");
                return new ResponseMessage { Message = "Card was deleted successfully", Status = true };
            }
            else
            {
                _logger.LogInformation($"Card was not deleted \n");
                return new ResponseMessage
                {
                    Message = "Kindly set a new card as " +
                    "primary card to delete present primary card"
                };
            }
        }

        /// <summary>
        /// Fetch debit card
        /// </summary>
        /// <param name="userId"></param>
        ///  /// <param name="email"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> GetCards(int userId, string email)
        {
            _logger.LogInformation($"Processing get Cards [UserId : {userId} | Email : {email}]\n");
            var profileCompletion = await _insuranceService.GetProfileCompletion(userId, email);
            if(profileCompletion is null)
            {
                _logger.LogInformation($"Get card failed [Reason : Could not get profile completion ]\n");
                return new ResponseMessage {Message = "Cannot fetch card now, please try again later", Status = false };
            }
            List<DebitCard> debitCard = new List<DebitCard>();

            if (profileCompletion.Data?.HealthInsuredPlan == HealthInsuredPlan.Individual)
            {
                _logger.LogInformation($"Get card for individual \n");
                var individualInsuranceUser = await _repositoryWrapper.InsuranceProfile.GetByUserIdAsync(userId);
                debitCard = individualInsuranceUser.Cards;
            }
            else if (profileCompletion.Data?.HealthInsuredPlan == HealthInsuredPlan.Corporate)
            {
                _logger.LogInformation($"Get card for individual \n");
                var coporateInsuranceUser = await _repositoryWrapper.CompanyProfile.GetCompanyProfileByUserId(userId);
                debitCard = coporateInsuranceUser.Cards;
            }
            else
            {
                _logger.LogInformation($"Get card for individual \n");
                var familyProfile = await _repositoryWrapper.FamilyProfile.GetByUserId(userId);
                debitCard = familyProfile.Cards;
            }
            if (debitCard.Count > 0)
            {
                _logger.LogInformation($"Get card for individual \n");
                var cardDTO = _mapper.Map<List<DebitCard>, List<CardDTO>>(debitCard);
                return new ResponseMessage { Data = cardDTO, Status = true, Message = "Card was fetched successfully" };
            }
            var emptyCardDTO = new List<CardDTO>();
            _logger.LogInformation($"Get card for individual [User has no cards] \n");
            return new ResponseMessage { Data = emptyCardDTO, Message = "User has no card, Kindly add a card", Status = true };
        }

        /// <summary>
        /// Service to change primary card
        /// </summary>
        /// <param name="newCardId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> ChangePrimaryCard(int newCardId, int userId)
        {
            _logger.LogInformation($"Change primary card processing [newCardId : {newCardId} | UserId : {userId}]\n");
            var presentPrimaryCard = await _repositoryWrapper.Card.GetPrimaryCard(userId);
            if (presentPrimaryCard == null)
            {
                _logger.LogInformation($"Change primary card terminated [Reason : present primary debit card not found]\n");
                return new ResponseMessage { Message = "You dont have a debit card, Kindly add one" };
            }

            var newPrimaryCard = await _repositoryWrapper.Card.GetCardByIdAsync(newCardId, userId);
            if (newPrimaryCard == null)
            {
                _logger.LogInformation($"Change primary card terminated [Reason : Secondary primary debit card not found]\n");
                return new ResponseMessage { Message = "Card was not found", ResponseCode = 12 };
            }
            if (newPrimaryCard.Status == (int)DebitCard_StatusValue.primary)
            {
                _logger.LogInformation($"Change primary card terminated [Reason : Preseumed Secondary primary is the primary card]\n");
                return new ResponseMessage { Message = "This card is presently the primary card" };
            }
            presentPrimaryCard.Status = (int)DebitCard_StatusValue.secondary;
            _repositoryWrapper.Card.Update(presentPrimaryCard);
            newPrimaryCard.Status = (int)DebitCard_StatusValue.primary;
            _repositoryWrapper.Card.Update(newPrimaryCard);

            if (newPrimaryCard.InsuranceUserProfileId != null)
            {
                _logger.LogInformation($"Change primary card for individual \n");
                var insuranceProfile = await _repositoryWrapper.InsuranceProfile.GetByUserIdAsync(userId);
                var activityLog = new ActivityLog(insuranceProfile.Id, null, null, "Change Primary Card", ServiceNames.HealthInsured.ToString());
                _repositoryWrapper.ActivityLog.Create(activityLog);
            }
            else if (newPrimaryCard.CompanyProfileId != null)
            {
                _logger.LogInformation($"Change primary card for company \n");
                var companyProfile = await _repositoryWrapper.CompanyProfile.GetCompanyProfileByUserId(userId);
                var activityLog = new ActivityLog(null, companyProfile.Id, null, "Change Primary Card", ServiceNames.HealthInsured.ToString());
                _repositoryWrapper.ActivityLog.Create(activityLog);
            }
            else
            {
                _logger.LogInformation($"Change primary card for family \n");
                var familyProfile = await _repositoryWrapper.FamilyProfile.GetByUserId(userId);
                var activityLog = new ActivityLog(null, null, familyProfile.Id, "Change Primary Card", ServiceNames.HealthInsured.ToString());
                _repositoryWrapper.ActivityLog.Create(activityLog);
            }
            _logger.LogInformation($"Change primary card was successfully \n");
            await _repositoryWrapper.Save();
            return new ResponseMessage { Message = "Primary card was changed successfully", Status = true };
        }

        /// <summary>
        /// Submit OTP
        /// </summary>
        /// <param name="otpViewModel"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> SubmitOtp(SetOtpViewModel otpViewModel, int id)
        {
            _logger.LogInformation($"Submit OTP [OTPViewModel : {JsonConvert.SerializeObject(otpViewModel)} | Id : {id}]\n");
            var profileCompletion = await _insuranceService.GetProfileCompletion(id, null);
            var paymentReference = await _repositoryWrapper.PaymentReference.GetByReference(otpViewModel.reference);
            if (profileCompletion.Data.HealthInsuredPlan == HealthInsuredPlan.Individual)
            {
                var insuranceProfile = await _repositoryWrapper.InsuranceProfile.GetByUserIdAsync(id);
                var checkprofileComplete = await _repositoryWrapper.InsuranceCompletionProfile.GetCompletionStateByUserId(id);

                var chargeCardResponse = await _paystackService.SendOtp(otpViewModel.otp, otpViewModel.reference, insuranceProfile.PhoneNumber
                       , insuranceProfile.DateOfBirth, otpViewModel.pin);
                return await ProcessPaystackChargeCardResponse(chargeCardResponse, insuranceProfile, checkprofileComplete,
                    paymentReference, otpViewModel.reference);
            }
            else if (profileCompletion.Data.HealthInsuredPlan == HealthInsuredPlan.Corporate)
            {
                var companyProfile = await _repositoryWrapper.CompanyProfile.GetCompanyProfileByUserId(id);
                var chargeCardResponse = await _paystackService.SendOtp(otpViewModel.otp, otpViewModel.reference, companyProfile.PhoneNumber
                       , DateTime.Now, otpViewModel.pin);
                return await ProcessPaystackChargeCardResponse(chargeCardResponse, companyProfile, paymentReference, otpViewModel.reference);
            }
            else if (profileCompletion.Data.HealthInsuredPlan == HealthInsuredPlan.Family)
            {
                var familyProfile = await _repositoryWrapper.FamilyProfile.GetExtendedFamilyDetails(id);
                var chargeCardResponse = await _paystackService.SendOtp(otpViewModel.otp, otpViewModel.reference, familyProfile.PhoneNumber
                       , DateTime.Now, otpViewModel.pin);
                return await ProcessPaystackChargeCardResponse(chargeCardResponse, familyProfile, paymentReference, otpViewModel.reference);
            }
            return new ResponseMessage { Message = "User does not have a profile,kindly create your profile", Status = false };
        }       

        /// <summary>
        /// This Method is when user tokenize their card. For a user adding card, Just a fee of 50 naira is deducted to process their card ,
        /// get an authorization code to use in future payments
        /// </summary>
        /// <param name="chargeCard"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> TokenizeCard(ChargeCardViewModel chargeCard, int id)
        {
            var profileCompletion = await _insuranceService.GetProfileCompletion(id, null);
            if (profileCompletion.Data.HealthInsuredPlan == HealthInsuredPlan.Individual)
            {
                _logger.LogInformation($"Process Indiviual card tokenization\n");
                return await ProcessIndividualCardTokenization(chargeCard, id);
            }
            else if (profileCompletion.Data.HealthInsuredPlan == HealthInsuredPlan.Corporate)
            {
                _logger.LogInformation($"Process corporate card tokenizatio\n");
                return await ProcessCorporateCardTokenization(chargeCard, id);
            }
            else if (profileCompletion.Data.HealthInsuredPlan == HealthInsuredPlan.Family)
            {
                _logger.LogInformation($"Process family card tokenization\n");
                return await ProcessFamilyCardTokenization(chargeCard, id);
            }
            return new ResponseMessage { Message = "User does not have a profile,kindly create your profile", Status = false };
        }

        /// <summary>
        /// This is used to process card tokenization for indidvidual users.This method process the charge amount and process api Request that would be sent to paystack.
        /// </summary>
        /// <param name="chargeCard"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private async Task<ResponseMessage> ProcessIndividualCardTokenization(ChargeCardViewModel chargeCard, int id)
        {
            var insuranceProfile = await _repositoryWrapper.InsuranceProfile.GetByUserIdAsync(id);
            var checkprofileComplete = await _repositoryWrapper.InsuranceCompletionProfile.GetCompletionStateByUserId(id);
            if (checkprofileComplete.ProfileCompleted == true)
            {
                var channel = insuranceProfile.InsuranceService.ToLower() == InsuranceProvider.Hygeia.ToString().ToLower() ? PaymentReference_ChannelValue.healthinsured_hygeia.ToString()
                   : PaymentReference_ChannelValue.healthinsured_axamansard.ToString();

                var amount = (50 * 100).ToString();

                var chargeCardModel = await GenerateChargeCardModel(chargeCard, id, insuranceProfile.Id, null, null, insuranceProfile.Email, amount, channel);
                if (!chargeCardModel.Status) return new ResponseMessage { Message = chargeCardModel.Message, Status = false };

                // Call Paystack service to charge user card
                var chargeCardResponse = await _paystackService.ChargeCard(chargeCardModel.Data.Item1, id, insuranceProfile.PhoneNumber, insuranceProfile.DateOfBirth);

                // function to process response from paystack
                return await ProcessPaystackChargeCardResponse(chargeCardResponse, insuranceProfile, checkprofileComplete
                    , chargeCardModel.Data.Item2, chargeCardModel.Data.Item3);
            }
            _logger.LogInformation($"Tokenization terminated. User does not have insurance profile\n");
            return new ResponseMessage { Message = "User has not been profiled,kindly create your profile", Status = false };
        }

        /// <summary>
        /// This is used to process card tokenization for corporate org.This method process the charge amount and process api Request that would be
        /// sent to paystack. 
        /// </summary>
        /// <param name="chargeCard"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private async Task<ResponseMessage> ProcessCorporateCardTokenization(ChargeCardViewModel chargeCard, int id)
        {
            var companyProfile = await _repositoryWrapper.CompanyProfile.GetCompanyProfileByUserId(id);
            if (companyProfile != null)
            {
                if (companyProfile.EmailConfirmed)
                {
                    string amount = "";

                    // if card is not tokenised and is adding beneficiaries, amount is debited for the beneficiaries
                    if (!companyProfile.TokenizationCompleted)
                    {
                        var beneficiaryReviews = await _repositoryWrapper.BeneficiaryReview.QueryableBeneficiaryReviews(id);
                        var totalAmount = beneficiaryReviews.Where(x => !x.IsRemove).Select(x => x.Amount).Sum();
                        if (totalAmount == 0)
                        {
                            return new ResponseMessage { Message = "Kindly add at least one beneficiary" };
                        }
                        amount = (totalAmount * 100).ToString();
                    }
                    else
                    {
                        // When user is adding card
                        amount = (100 * 50).ToString();
                    }

                    // if company phonenumber is null, use the phonenumber provided during registration
                    // previous version company was not required to provide phonenumber
                    if (companyProfile.PhoneNumber is null)
                    {
                        var user = await _repositoryWrapper.ApplicationUser.FindByIdAsync(id);
                        companyProfile.PhoneNumber = user.PhoneNumber;
                    }

                    var channel = companyProfile.InsuranceService.ToLower() == InsuranceProvider.Hygeia.ToString().ToLower() ? PaymentReference_ChannelValue.healthinsured_hygeia.ToString()
                      : PaymentReference_ChannelValue.healthinsured_axamansard.ToString();

                    var chargeCardModel = await GenerateChargeCardModel(chargeCard, id, null, companyProfile.Id, null, companyProfile.CompanyEmail,amount, channel);
                    if (!chargeCardModel.Status) return new ResponseMessage { Message = chargeCardModel.Message, Status = false };

                    // Paystack service to charge user card
                    var chargeCardResponse = await _paystackService.ChargeCard(chargeCardModel.Data.Item1, id, companyProfile.PhoneNumber, DateTime.Now);

                    // function to process response from paystack
                    return await ProcessPaystackChargeCardResponse(chargeCardResponse, companyProfile, chargeCardModel.Data.Item2, chargeCardModel.Data.Item3);
                }
                return new ResponseMessage { Message = "Kindly update your profile", Status = false };
            }
            return new ResponseMessage { Message = "User does not have a profile,kindly create your profile", Status = false };
        }

        /// <summary>
        /// This is used to process card tokenization for family profiles.
        /// </summary>
        /// <param name="chargeCard"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private async Task<ResponseMessage> ProcessFamilyCardTokenization(ChargeCardViewModel chargeCard, int id)
        {
            var familyProfile = await _repositoryWrapper.FamilyProfile.GetExtendedFamilyDetails(id);
            if (familyProfile != null)
            {
                if (familyProfile.EmailConfirmed)
                {
                    var channel = familyProfile.InsuranceService.ToLower() == InsuranceProvider.Hygeia.ToString().ToLower() ? PaymentReference_ChannelValue.healthinsured_hygeia.ToString() 
                        : PaymentReference_ChannelValue.healthinsured_axamansard.ToString();

                    var amount = (50 * 100).ToString();

                    var chargeCardModel = await GenerateChargeCardModel(chargeCard, id, null, null, familyProfile.Id, familyProfile.Email, amount, channel);
                    if (!chargeCardModel.Status) return new ResponseMessage { Message = chargeCardModel.Message, Status = false };

                    // Paystack service to charge user card
                    var chargeCardResponse = await _paystackService.ChargeCard(chargeCardModel.Data.Item1, id, familyProfile.PhoneNumber, familyProfile.DateCreated);

                    // function to process response from paystack
                    return await ProcessPaystackChargeCardResponse(chargeCardResponse, familyProfile, chargeCardModel.Data.Item2, chargeCardModel.Data.Item3);
                }
                return new ResponseMessage { Message = "Kindly update your profile", Status = false };
            }
            return new ResponseMessage { Message = "User does not have a profile,kindly create your profile", Status = false };
        }

        private async Task<ResponseMessage<(ChargeCard, PaymentReference, string)>> GenerateChargeCardModel(ChargeCardViewModel chargeCard, int userId, int? insuranceProfileId,
            int? companyProfileId, int? familyProfileId, string email, string amount, string channel )
        {
            // remove empty space from the card.
            var cardNumber = chargeCard.card.number.Replace(" ", "");

            // Check if user has card that matches last four card digit
            var checkIfCardWasPreviouslyTokenized = await _repositoryWrapper.Card.CheckIfCardWasPreviouslyTokenized(userId, cardNumber[^4..]);
            if (checkIfCardWasPreviouslyTokenized != null)
            {
                _logger.LogInformation($"Card was tokenised previously\n");
                return new ResponseMessage<(ChargeCard, PaymentReference, string)> { Message = "This card was previously tokenized", Status= false };
            }

            var chargeCardRequest = _mapper.Map<API_RequestModel.Paystack.Card>(chargeCard.card);

            var card = new ChargeCard
            {
                card = chargeCardRequest,
                email = email,
                reference = Guid.NewGuid().ToString(),
                pin = chargeCard.pin
            };

            card.amount = amount;
            var amountInDecimal = decimal.Parse(amount) / 100;

            // Create payment reference for the charge.

            var paymentReference = new PaymentReference(channel, card.reference, insuranceProfileId, companyProfileId, familyProfileId, userId
            , amountInDecimal, PaymentReference_StatusValue.Pending.ToString());
            _repositoryWrapper.PaymentReference.Create(paymentReference);
            await _repositoryWrapper.Save();
            return new ResponseMessage<(ChargeCard, PaymentReference, string)> { Status = true, Data = (card, paymentReference, card.reference)};
        }

        /// <summary>
        /// This is an overloaded method, which handle paystack charge card response for individual users.It is responsible for processing the logic and response
        /// if the charge was successful or not.
        /// It saves the debit card and calls the refund method if the charge was just a charge to tokenize the card.
        /// Also its responsible for calling the method which enroll users to the insurance provider when user tokenize card for the first time.
        /// </summary>
        /// <param name="chargeCardResponse"></param>
        /// <param name="insuranceUserProfile"></param>
        /// <param name="checkprofileComplete"></param>
        /// <param name="paymentReference"></param>
        /// <param name="cardReference"></param>
        /// <returns></returns>
        private async Task<ResponseMessage> ProcessPaystackChargeCardResponse(TokenizationResponse chargeCardResponse, InsuranceUserProfile insuranceUserProfile,
            InsuranceCompletionProfile checkprofileComplete, PaymentReference paymentReference, string cardReference)
        {
            // if charge card was successfully
            if (chargeCardResponse.Status == true && chargeCardResponse.ResponseCode == 0)
            {
                _logger.LogInformation($"Process Successful paystack tokenization response\n");
                int cardStatus = 0;
                //If card count is 0. it means there is no card available, so the card tokenised will
                //be the primary card so primary card status is set to 1 
                //else, card status is 0;
                if (!(insuranceUserProfile.Cards.Any(x => x.Status == (int)DebitCard_StatusValue.primary)) || insuranceUserProfile.Cards.Count == 0)
                {
                    cardStatus = (int)DebitCard_StatusValue.primary;
                }

                var debitCard = new DebitCard(insuranceUserProfile.UserId.Value, insuranceUserProfile.Id, null, null, cardStatus, chargeCardResponse.LastDigit, chargeCardResponse.Type
                    , cardReference, chargeCardResponse.AuthorizationCode);
                _repositoryWrapper.Card.Create(debitCard);

                var activityLog = new ActivityLog(insuranceUserProfile.Id, null, null, "Debit Card Added", ServiceNames.HealthInsured.ToString());
                _repositoryWrapper.ActivityLog.Create(activityLog);

                checkprofileComplete.TokenizationCompleted = true;
                _repositoryWrapper.InsuranceProfile.Update(insuranceUserProfile);
                await _repositoryWrapper.Save();

                if (insuranceUserProfile.SubscriptionStatus is null)
                {
                    await Process_SuccessfulInsuranceIndividualPayment_FirstTimePayment(insuranceUserProfile);
                    await _hmoIntegrationService.SendDetailsToInsuranceProvider(insuranceUserProfile);
                }

                BackgroundJob.Enqueue(() => _paystackService.RefundTestCardFunds(cardReference, (50 * 100).ToString()));
                return new ResponseMessage
                {
                    Data = chargeCardResponse,
                    Status = chargeCardResponse.Status,
                    ResponseCode = chargeCardResponse.ResponseCode,
                    Message = chargeCardResponse.Message
                };
            }
            _logger.LogInformation($"Process Not Successful Paystack tokenization response\n");
            // Call method to process other transaction instance that is not successfull e.g Send_Otp,Send_Url,Failed.
            return await ProcessNotSuccessfulPaystackChargeCardResponse(paymentReference, chargeCardResponse);
        }

        public async Task Process_SuccessfulInsuranceIndividualPayment_FirstTimePayment(InsuranceUserProfile insuranceUserProfile)
        {
            _logger.LogInformation($"Process successful first time individual payment [Insuracen profile :{JsonConvert.SerializeObject(insuranceUserProfile)}]\n");

            insuranceUserProfile.EndActiveStatusDate = DateTime.Now.AddDays(SubscriptionAccessor.FreeTrialDayDuration);
            insuranceUserProfile.TransId = (insuranceUserProfile.InsuranceService == null | insuranceUserProfile.InsuranceService == InsuranceProvider.Axamansard.ToString()) ? _uniqueIdentifier.GetUniqueCode(10) : "";

            // Schedule debit email reminder for user 
            insuranceUserProfile.PendingEmailJobId = BackgroundJob.Schedule(() => _insuranceService.SendEmailReminder(insuranceUserProfile.Email, insuranceUserProfile.Surname, "", null),
                    DateTime.Now.AddDays(SubscriptionAccessor.FreeTrialDayDuration).Subtract(new TimeSpan(3, 0, 0, 0)));

            //Schedule job to debit user every 28 days
            insuranceUserProfile.PendingJobId = _tokenizationService.ProcessScheduledPayment(insuranceUserProfile);

            var checkprofileComplete = await _repositoryWrapper.InsuranceCompletionProfile.GetCompletionStateByUserId(insuranceUserProfile.UserId.Value);
            checkprofileComplete.TokenizationCompleted = true;
            _repositoryWrapper.InsuranceCompletionProfile.Update(checkprofileComplete);

            _emailSender.HealthInsuredSubscriptionMail(insuranceUserProfile.Email, "Active Free Trial", insuranceUserProfile.Surname, insuranceUserProfile.TransId,
                insuranceUserProfile.CareProviderName, insuranceUserProfile.PlanCode);

            insuranceUserProfile.SubscriptionStatus = true;
            insuranceUserProfile.ActiveStatus = true;
            insuranceUserProfile.StartActiveStatusDate = DateTime.Now;

            _repositoryWrapper.InsuranceProfile.Update(insuranceUserProfile);

            //Create Audit thats user subscrption changed 
            var activityLog = new ActivityLog(insuranceUserProfile.Id, null, null, "Subscription was activated", ServiceNames.HealthInsured.ToString());
            _repositoryWrapper.ActivityLog.Create(activityLog);
            await _repositoryWrapper.Save();
            _logger.LogInformation($"First time individual payment ewas successful\n");
        }

        private async Task<ResponseMessage> ProcessPaystackChargeCardResponse(TokenizationResponse chargeCardResponse, FamilyProfile familyProfile, PaymentReference paymentReference
            , string cardReference)
        {
            // if charge card was successfully
            if (chargeCardResponse.Status == true && chargeCardResponse.ResponseCode == 0)
            {
                int cardStatus = 0;
                //If card count is 0. it means there is no card available, so the card tokenised will
                //be the primary card so primary card status is set to 1 
                //else, card status is 0;
                if (!(familyProfile.Cards.Any(x => x.Status == (int)DebitCard_StatusValue.primary)) || familyProfile.Cards.Count == 0)
                {
                    cardStatus = (int)DebitCard_StatusValue.primary;
                }

                var debitCard = new DebitCard(familyProfile.UserId, null, null, familyProfile.Id, cardStatus, chargeCardResponse.LastDigit, chargeCardResponse.Type
                    , cardReference, chargeCardResponse.AuthorizationCode);
                _repositoryWrapper.Card.Create(debitCard);

                if (!familyProfile.TokenizationCompleted)
                {
                    familyProfile.TokenizationCompleted = true;
                    _repositoryWrapper.FamilyProfile.Update(familyProfile);
                    _logger.LogCritical(" About to hit process insurance for family payment");
                    if (familyProfile.InsuranceUserProfiles != null && familyProfile.InsuranceUserProfiles.Count > 0)
                    {
                        _logger.LogCritical("process insurance for family payment");
                        await FamilyMembersActivation(familyProfile);
                        _familyInsurance.FamilySubscription(familyProfile.Email, "Active Subscriptions", familyProfile.FullName);
                    }
                }
                var activityLog = new ActivityLog(null, null, familyProfile.Id, "Debit Card Added", ServiceNames.HealthInsured.ToString());
                _repositoryWrapper.ActivityLog.Create(activityLog);
                await _repositoryWrapper.Save();

                BackgroundJob.Enqueue(() => _paystackService.RefundTestCardFunds(cardReference, (50 * 100).ToString()));

                return new ResponseMessage
                {
                    Data = chargeCardResponse,
                    Status = chargeCardResponse.Status,
                    ResponseCode = chargeCardResponse.ResponseCode,
                    Message = chargeCardResponse.Message
                };
            }
            return await ProcessNotSuccessfulPaystackChargeCardResponse(paymentReference, chargeCardResponse);
        }

        /// <summary>
        /// This is an overloaded method, which handle paystack charge card response for corporate org.It is responsible for processing the logic and response
        /// if the charge was successful or not.
        /// </summary>
        /// <param name="chargeCardResponse"></param>
        /// <param name="companyProfile"></param>
        /// <param name="paymentReference"></param>
        /// <param name="cardReference"></param>
        /// <returns></returns>
        private async Task<ResponseMessage> ProcessPaystackChargeCardResponse(TokenizationResponse chargeCardResponse, CompanyProfile companyProfile,
           PaymentReference paymentReference, string cardReference)
        {
            // if charge card was successfully
            if (chargeCardResponse.Status == true && chargeCardResponse.ResponseCode == 0)
            {
                int cardStatus = 0;
                //If card count is 0. it means there is no card available, so the card tokenised will
                //be the primary card so primary card status is set to 1 
                //else, card status is 0;
                if (!(companyProfile.Cards.Any(x => x.Status == (int)DebitCard_StatusValue.primary)) || companyProfile.Cards.Count == 0)
                {
                    cardStatus = (int)DebitCard_StatusValue.primary;
                }

                var debitCard = new DebitCard(companyProfile.UserId, null, companyProfile.Id, null, cardStatus, chargeCardResponse.LastDigit, chargeCardResponse.Type
                    , cardReference, chargeCardResponse.AuthorizationCode);
                _repositoryWrapper.Card.Create(debitCard);

                if (!companyProfile.TokenizationCompleted)
                {
                    companyProfile.TokenizationCompleted = true;
                    _repositoryWrapper.CompanyProfile.Update(companyProfile);

                }
                var activityLog = new ActivityLog(null, companyProfile.Id, null, "Debit Card Added", ServiceNames.HealthInsured.ToString());
                _repositoryWrapper.ActivityLog.Create(activityLog);
                await _repositoryWrapper.Save();

                /// We sleep the task for 10 seconds so we can process the paystack webhook  <see cref="ProcessPaystackWebHook(string, string, string, string, string, string, string)"/>
                await Task.Delay(10000);

                return new ResponseMessage
                {
                    Data = chargeCardResponse,
                    Status = chargeCardResponse.Status,
                    ResponseCode = chargeCardResponse.ResponseCode,
                    Message = chargeCardResponse.Message
                };
            }
            return await ProcessNotSuccessfulPaystackChargeCardResponse(paymentReference, chargeCardResponse);
        }

        private async Task<ResponseMessage> ProcessNotSuccessfulPaystackChargeCardResponse(PaymentReference paymentReference, TokenizationResponse chargeCardResponse)
        {
            // If charge card response request for OTP
            if (chargeCardResponse.Status == true && chargeCardResponse.ResponseCode == 12)
            {
                paymentReference.Status = PaymentReference_StatusValue.Send_Otp.ToString();
                _repositoryWrapper.PaymentReference.Update(paymentReference);
                await _repositoryWrapper.Save();
                return new ResponseMessage
                {
                    Data = chargeCardResponse.Data,
                    Message = chargeCardResponse.Message,
                    Status = chargeCardResponse.Status,
                    ResponseCode = chargeCardResponse.ResponseCode
                };
            }
            // If charge card response wants to redirect
            else if (chargeCardResponse.Status == true && chargeCardResponse.ResponseCode == 20)
            {
                paymentReference.Status = PaymentReference_StatusValue.Send_Url.ToString();
                _repositoryWrapper.PaymentReference.Update(paymentReference);
                await _repositoryWrapper.Save();
                return new ResponseMessage
                {
                    Message = chargeCardResponse.Message,
                    Status = chargeCardResponse.Status,
                    ResponseCode = chargeCardResponse.ResponseCode,
                    Data = chargeCardResponse
                };
            }
            // If theres is an error
            paymentReference.Status = PaymentReference_StatusValue.Failed.ToString();
            _repositoryWrapper.PaymentReference.Update(paymentReference);
            await _repositoryWrapper.Save();
            return new ResponseMessage
            {
                Data = chargeCardResponse.Data,
                Message = chargeCardResponse.Message,
                Status = chargeCardResponse.Status,
                ResponseCode = chargeCardResponse.ResponseCode
            };
        }

        /// <summary>
        /// method to cancel individual users subcription
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> CancelSubscription(int userId, string reason)
        {
            var insuranceProfile = await _repositoryWrapper.InsuranceProfile.GetByUserIdAsync(userId);
            if (insuranceProfile.InsurancePayeeId != null)
            {
                return new ResponseMessage { Status = true, Message = "Please Contact Payee To Cancel Subscription!" };
            }
            return await ProcessCancelSubscription(insuranceProfile);
        }

        private async Task<ResponseMessage> ProcessCancelSubscription(InsuranceUserProfile insuranceProfile)
        {
            _logger.LogInformation($"Process Cancel Subscription [Payload : {JsonConvert.SerializeObject(insuranceProfile)}]");
            if (insuranceProfile.SubscriptionStatus == false || insuranceProfile.SubscriptionStatus is null)
            {
                return new ResponseMessage { Message = "Insurance Profile has no subscription", Status = false };
            }

            //if (insuranceProfile.TransId == null || insuranceProfile.TransId == "" || insuranceProfile.TransId == "Pending")
            //{
            //    return new ResponseMessage { Message = "Subscription can only be cancelled after enrollee number has been processed. this should take less than 2 working days" };
            //}

            var scheduledJobId = insuranceProfile.PendingJobId;

            BackgroundJob.Delete(scheduledJobId);
            _logger.LogInformation($"Insurance profile of Id {insuranceProfile.Id} backgroud job was deleted[Scheduled Debit background Job Id {scheduledJobId}]\n");

            if (insuranceProfile.PendingEmailJobId != null)
            {
                BackgroundJob.Delete(insuranceProfile.PendingEmailJobId);
            }

            var daysToCancelUserActivityStatus = insuranceProfile.EndActiveStatusDate;
            // Schedule task to render user status inactive when cycle ends
            var jobId = BackgroundJob.Schedule(() => _tokenizationService.ProcessUserActiveStatusCancellation(insuranceProfile.Id, null), daysToCancelUserActivityStatus);

            insuranceProfile.PendingJobId = jobId;
            insuranceProfile.PendingEmailJobId = null;
            insuranceProfile.SubscriptionStatus = false;
            _repositoryWrapper.InsuranceProfile.Update(insuranceProfile);

            var activityLog = new ActivityLog(insuranceProfile.Id, null, null, "Subscription Was Cancelled Successfully", ServiceNames.HealthInsured.ToString());
            _repositoryWrapper.ActivityLog.Create(activityLog);

            await _repositoryWrapper.Save();
            var profileDTO = _mapper.Map<IndividualProfileDTO>(insuranceProfile);
            return new ResponseMessage
            {
                Data = profileDTO,
                Message = "Subscription was canceled successfully.However profile will remain active till " +
                "insurance cycle ends.",
                Status = true
            };
        }

        public async Task FamilyMembersActivation(FamilyProfile familyProfile)
        {
            var insuranceProfiles = familyProfile.InsuranceUserProfiles;
            foreach (var insuranceUserProfile in insuranceProfiles)
            {
                insuranceUserProfile.StartActiveStatusDate = insuranceUserProfile.EndActiveStatusDate != default ? insuranceUserProfile.EndActiveStatusDate : DateTime.Now;

                insuranceUserProfile.EndActiveStatusDate = insuranceUserProfile.EndActiveStatusDate != default ? insuranceUserProfile.EndActiveStatusDate.AddDays(SubscriptionAccessor.FreeTrialDayDuration)
                    : DateTime.Now.AddDays(SubscriptionAccessor.FreeTrialDayDuration);

                // Schedule debit email reminder for user 
                insuranceUserProfile.PendingEmailJobId = BackgroundJob.Schedule(() => _familyInsurance.SendPaymentReminder(familyProfile.Email, familyProfile.FullName,
                    $"{insuranceUserProfile.Othernames} {insuranceUserProfile.Surname}", null),
                        DateTime.Now.AddDays(SubscriptionAccessor.FreeTrialDayDuration).Subtract(new TimeSpan(3, 0, 0, 0)));

                //Schedule job to debit user every 28 days
                insuranceUserProfile.PendingJobId = _tokenizationService.ProcessScheduledPayment(insuranceUserProfile);

                insuranceUserProfile.TransId = (insuranceUserProfile.InsuranceService == null | insuranceUserProfile.InsuranceService == InsuranceProvider.Axamansard.ToString())
                ? _uniqueIdentifier.GetUniqueCode(10) : "Pending";
                insuranceUserProfile.SubscriptionStatus = true;
                insuranceUserProfile.ActiveStatus = true;

                _repositoryWrapper.InsuranceProfile.Update(insuranceUserProfile);
                await _repositoryWrapper.Save();
            }
            BackgroundJob.Enqueue(() => SendInsuranceListToInsuranceProvider(insuranceProfiles));
        }

        private async Task SendInsuranceListToInsuranceProvider(List<InsuranceUserProfile> insuranceUserProfiles)
        {
            foreach (var insuranceUserProfile in insuranceUserProfiles)
            {
                await _hmoIntegrationService.SendDetailsToInsuranceProvider(insuranceUserProfile);
            }
        }

        /// <summary>
        /// Activate a new family member with the family primary debit card
        /// </summary>
        /// <param name="familyUserId"></param>
        /// <param name="insuranceProfileId"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> ActivateFamilyMemberWithPrimaryCard(int familyUserId, int insuranceProfileId)
        {
            var familyProfile = await _repositoryWrapper.FamilyProfile.GetExtendedFamilyDetails(familyUserId);
            var insuranceProfile = familyProfile.InsuranceUserProfiles.Where(x => x.Id == insuranceProfileId).FirstOrDefault();
            if (insuranceProfile != null)
            {
                if (insuranceProfile.SubscriptionStatus == true)
                {
                    return new ResponseMessage { Message = "Subscription is currently active", Status = false };
                }
                var primaryCard = familyProfile.Cards.FirstOrDefault(x => x.Status == (int)DebitCard_StatusValue.primary);
                if (primaryCard == null)
                {
                    return new ResponseMessage { Message = "Kindly add a primary card, then start the activation process" };
                }
                if (String.IsNullOrWhiteSpace(insuranceProfile.TransId) || String.IsNullOrEmpty(insuranceProfile.TransId))
                {
                    if (insuranceProfile.InsuranceService == InsuranceProvider.Axamansard.ToString())
                    {
                        insuranceProfile.TransId = _uniqueIdentifier.GetUniqueCode(10);
                    }
                }
                // If  user is not in an active cycle
                if (insuranceProfile.ActiveStatus == false || insuranceProfile.ActiveStatus is null)
                {
                    var response = await ProcessImmediateReactivationPayment(insuranceProfile, familyProfile.UserId, primaryCard.Authorization_Code);
                    if (response.Status)
                    {
                        // Schedule debit email reminder for user 
                        insuranceProfile.PendingEmailJobId = BackgroundJob.Schedule(() => _familyInsurance.SendPaymentReminder(familyProfile.Email, familyProfile.FullName
                            , insuranceProfile.Surname + " " + insuranceProfile.Othernames, null), insuranceProfile.EndActiveStatusDate.Subtract(new TimeSpan(3, 0, 0, 0)));
                        _repositoryWrapper.InsuranceProfile.Update(insuranceProfile);
                        await _repositoryWrapper.Save();
                    }
                    return response;
                }
                // If user is in an active cycle
                else
                {
                    var response = await ProcessScheduledReactivationFlow(insuranceProfile, insuranceProfile.EndActiveStatusDate);
                    if (response.Status)
                    {
                        // Schedule debit email reminder for user 
                        insuranceProfile.PendingEmailJobId = BackgroundJob.Schedule(() => _familyInsurance.SendPaymentReminder(familyProfile.Email, familyProfile.FullName
                            , insuranceProfile.Surname + " " + insuranceProfile.Othernames, null), insuranceProfile.EndActiveStatusDate.Subtract(new TimeSpan(3, 0, 0, 0)));
                        _repositoryWrapper.InsuranceProfile.Update(insuranceProfile);
                        await _repositoryWrapper.Save();
                    }
                    return response;
                }
            }
            return new ResponseMessage { Message = "Insurance profile does not exist under your family profile!" };
        }

        public async Task<ResponseMessage> DeactivateFamilyMember(InsuranceUserProfile insuranceProfile)
        {
            return await ProcessCancelSubscription(insuranceProfile);
        }

        /// <summary>
        /// Reactivate user Subscription with user primary card.
        /// If user Subscription status is false and cycle is active. Background job is scheduled to reactivate the user when the cysle ends.
        /// Else user subscription is reactivated immediately
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> ReactivateWithPresentPrimaryCard(int userId)
        {
            var insuranceProfile = await _repositoryWrapper.InsuranceProfile.GetByUserIdAsync(userId);
            if (insuranceProfile.SubscriptionStatus == true)
            {
                return new ResponseMessage { Message = "Subscription is currently active", Status = false };
            }
            var primaryCard = insuranceProfile.Cards.FirstOrDefault(x => x.Status == (int)DebitCard_StatusValue.primary);
            if (primaryCard == null)
            {
                return new ResponseMessage { Message = "Kindly add a primary card, then start the reactivation process" };
            }

            // If  user is not in an active cycle
            if (insuranceProfile.ActiveStatus == false)
            {
                return await ProcessImmediateReactivationPayment(insuranceProfile, insuranceProfile.UserId.Value, primaryCard.Authorization_Code);
            }
            // If user is in an active cycle
            else
            {
                // We pass  the time 
                return await ProcessScheduledReactivationFlow(insuranceProfile, insuranceProfile.EndActiveStatusDate);
            }
        }

        /// <summary>
        /// Func to process immediate user reactivation
        /// </summary>
        /// <param name="userAxamansardProfile"></param>
        /// <param name="authorization_Code"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> ProcessImmediateReactivationPayment(InsuranceUserProfile insuranceProfile, int userId, string authorization_Code)
        {
            _logger.LogInformation($"ProcessImmediateReactivationPayment [UserId : {userId} | InsuranceProfile : {JsonConvert.SerializeObject(insuranceProfile)}]\n");
            FamilyProfile family = null;
            InsuranceUserProfile payee = null;
            var chageAuthorizationModel = new ChargeAuthorization()
            {
                email = insuranceProfile.Email,
                amount = (insuranceProfile.Premium * 100).ToString(),
                authorization_code = authorization_Code
            };

            if (insuranceProfile.FamilyProfileId != null)
            {
                family = await _repositoryWrapper.FamilyProfile.GetFamilyByFamilyId(insuranceProfile.FamilyProfileId.Value);
                chageAuthorizationModel.email = family.Email;
            }
            else if (insuranceProfile.InsurancePayeeId != null)
            {
                payee = await _repositoryWrapper.InsuranceProfile.GetByUserIdAsync(userId);
                chageAuthorizationModel.email = payee.Email;
            }

            // Call Paystack service. Debit user using card authorization code
            var chargeAuthorization = await _paystackService.ChargeAuthorization(chageAuthorizationModel);
            if (chargeAuthorization.Status)
            {
                await Process_SuccessfulInsuranceIndividualPayment_ImmediateReactivationPayment(insuranceProfile, family, payee);
                await _hmoIntegrationService.SendDetailsToInsuranceProvider(insuranceProfile);
                return new ResponseMessage { Message = "Reactivation was successful.", Status = true, ResponseCode = chargeAuthorization.ResponseCode };
            }
            // Setfailed payment reference for transacation;
            var channel = insuranceProfile.InsuranceService == InsuranceProvider.Hygeia.ToString() ? PaymentReference_ChannelValue.healthinsured_hygeia.ToString()
                : PaymentReference_ChannelValue.healthinsured_axamansard.ToString();

            if (insuranceProfile.FamilyProfileId is null)
            {
                var paymentReference = new PaymentReference(channel, chargeAuthorization.Reference, insuranceProfile.Id, null, null, userId
                   , insuranceProfile.Premium, PaymentReference_StatusValue.Failed.ToString());
                _repositoryWrapper.PaymentReference.Create(paymentReference);
            }
            else
            {
                var paymentReference = new PaymentReference(channel, chargeAuthorization.Reference, null, null, insuranceProfile.FamilyProfileId, userId
                  , insuranceProfile.Premium, PaymentReference_StatusValue.Failed.ToString());
                _repositoryWrapper.PaymentReference.Create(paymentReference);
            }

            await _repositoryWrapper.Save();

            return new ResponseMessage { Message = chargeAuthorization.Message, Status = false, ResponseCode = chargeAuthorization.ResponseCode };
        }

        private async Task Process_SuccessfulInsuranceIndividualPayment_ImmediateReactivationPayment(InsuranceUserProfile insuranceUserProfile, FamilyProfile family, 
            InsuranceUserProfile payee)
        {
            insuranceUserProfile.StartActiveStatusDate = DateTime.Now;

            insuranceUserProfile.EndActiveStatusDate = DateTime.Now.AddDays(SubscriptionAccessor.FreeTrialDayDuration);

            //Schedule job to debit user next 28 days
            insuranceUserProfile.PendingJobId = _tokenizationService.ProcessScheduledPayment(insuranceUserProfile);

            // Schedule debit email reminder for user 
            if (insuranceUserProfile.FamilyProfileId != null)
            {
                insuranceUserProfile.PendingEmailJobId = BackgroundJob.Schedule(() => _familyInsurance.SendPaymentReminder(family.Email, family.FullName
                       , insuranceUserProfile.Surname + " " + insuranceUserProfile.Othernames, null), insuranceUserProfile.EndActiveStatusDate.Subtract(new TimeSpan(3, 0, 0, 0)));
            }
            else if (insuranceUserProfile.InsurancePayeeId != null)
            {
                insuranceUserProfile.PendingEmailJobId = BackgroundJob.Schedule(() => _tokenizationService.SendEmailReminder(payee.Email, payee.Surname, "for your friend " + insuranceUserProfile.Surname, null)
                , insuranceUserProfile.EndActiveStatusDate.Subtract(new TimeSpan(3, 0, 0, 0)));
            }
            else
            {
                insuranceUserProfile.PendingEmailJobId = BackgroundJob.Schedule(() => _tokenizationService.SendEmailReminder(insuranceUserProfile.Email, insuranceUserProfile.Surname, "", null)
                , insuranceUserProfile.EndActiveStatusDate.Subtract(new TimeSpan(3, 0, 0, 0)));
            }

            insuranceUserProfile.SubscriptionStatus = true;
            insuranceUserProfile.ActiveStatus = true;
            insuranceUserProfile.StartActiveStatusDate = DateTime.Now;
            _repositoryWrapper.InsuranceProfile.Update(insuranceUserProfile);

            //Create Audit thats user subscrption changed 
            var activityLog = new ActivityLog(insuranceUserProfile.Id, null, null, "Subscription was activated", ServiceNames.HealthInsured.ToString());
            _repositoryWrapper.ActivityLog.Create(activityLog);

            await _repositoryWrapper.Save();
        }

        public async Task<ResponseMessage> ActivateRefereeWithPrimaryCard(int userId, int refereeInsuranceId)
        {
            var payeeInsuranceProfile = await _repositoryWrapper.InsuranceProfile.GetByUserIdAsync(userId);
            var refereedInsuranceProfile = await _repositoryWrapper.InsuranceProfile.GetByIdAsync(refereeInsuranceId);
            if (refereedInsuranceProfile != null)
            {
                if (refereedInsuranceProfile.SubscriptionStatus == true)
                {
                    return new ResponseMessage { Message = "Subscription is currently active", Status = false };
                }
                var primaryCard = payeeInsuranceProfile.Cards.FirstOrDefault(x => x.Status == (int)DebitCard_StatusValue.primary);
                if (primaryCard == null)
                {
                    return new ResponseMessage { Message = "Kindly add a primary card, then start the activation process" };
                }
                // If  user is not in an active cycle
                if (refereedInsuranceProfile.ActiveStatus == false)
                {
                    var response = await ProcessImmediateReactivationPayment(refereedInsuranceProfile, userId, primaryCard.Authorization_Code);
                    if (response.Status)
                    {
                        _repositoryWrapper.InsuranceProfile.Update(refereedInsuranceProfile);
                        await _repositoryWrapper.Save();
                    }
                    return response;
                }
                // If user is in an active cycle
                else
                {
                    var response = await ProcessScheduledReactivationFlow(refereedInsuranceProfile, refereedInsuranceProfile.EndActiveStatusDate);
                    if (response.Status)
                    {
                        _repositoryWrapper.InsuranceProfile.Update(refereedInsuranceProfile);
                        await _repositoryWrapper.Save();
                    }
                    return response;
                }
            }
            return new ResponseMessage { Message = "Insurance profile does not exist under your family profile!" };
        }

        /// <summary>
        /// Func to process user activation flow.
        /// Determines maybe user is activated immediately or a background job is scheduled for reactivation
        /// If user reacytivates within an active cycle, user is just scheduled for payment again when the cycle ends.
        /// </summary>
        /// <param name="userAxamansardProfile"></param>
        /// <param name="authorization_Code"></param>
        /// <param name="reactivationTime"></param>
        /// <returns></returns>
        private async Task<ResponseMessage> ProcessScheduledReactivationFlow(InsuranceUserProfile insuranceProfile, DateTime? reactivationTime)
        {
            // If user is still in active cycle and want to reactivate, that means user must have a background job scheduled to render user INACTIVE
            // at the end of cycle.
            // To resolve this, we delete background task scheduled to render user inactive at the end of current cycle          
            BackgroundJob.Delete(insuranceProfile.PendingJobId);

            // Task to process scheduled payment at end of current active cycle
            var jobId = _tokenizationService.ProcessScheduledPayment(insuranceProfile);

            // Schedule debit email reminder for user 
            if (insuranceProfile.FamilyProfileId is null && insuranceProfile.InsurancePayeeId is null)
            {
                insuranceProfile.PendingEmailJobId = BackgroundJob.Schedule(() => _tokenizationService.SendEmailReminder(insuranceProfile.Email, insuranceProfile.Surname, "", null), insuranceProfile.EndActiveStatusDate.Subtract(new TimeSpan(3, 0, 0, 0)));
            }

            insuranceProfile.SubscriptionStatus = true;
            insuranceProfile.ActiveStatus = true;
            insuranceProfile.PendingJobId = jobId;
            _repositoryWrapper.InsuranceProfile.Update(insuranceProfile);
            await _repositoryWrapper.Save();
            return new ResponseMessage
            {
                Status = true,
                Message = "Activation was successful.You will be debited at the end of " +
                "active cycle"
            };
        }

        /// <summary>
        /// method to cancel corporate insurance users subcription
        /// </summary>
        /// <param name="beneficiaryListViewModel"></param>
        /// <param name="companyUserId"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> DeactivateCompanyBeneficiaries(BeneficiaryListViewModel beneficiaryListViewModel, int companyUserId)
        {
            var companyProfile = await _repositoryWrapper.CompanyProfile.GetCompanyProfileByUserId(companyUserId);
            foreach (var item in beneficiaryListViewModel.Emails)
            {
                var insuranceUserProfile = await _repositoryWrapper.InsuranceProfile.GetByEmail(item);
                if (companyProfile.InsuranceUserProfiles.Contains(insuranceUserProfile))
                {
                    if (insuranceUserProfile.CompanySubscribedStatus.ToLower() == InsuranceProfile_CompanySubStatusValue.Inactive.ToString().ToLower())
                    {
                        return new ResponseMessage { Data = "Beneficiary was previously deactivated, beneficiary will remain active and would be moved to the inactive list at the end of current cycle.", Status = true };
                    }

                    if (insuranceUserProfile.CompanySubscribedStatus.ToLower() == InsuranceProfile_CompanySubStatusValue.Active.ToString().ToLower())
                    {
                        insuranceUserProfile.CompanySubscribedStatus = InsuranceProfile_CompanySubStatusValue.Inactive.ToString();
                        insuranceUserProfile.SubscriptionStatus = false;
                        BackgroundJob.Schedule(() => _tokenizationService.ProcessUserActiveStatusCancellation(insuranceUserProfile.UserId.Value), companyProfile.NextPaymentDate.Value);
                    }
                    _repositoryWrapper.InsuranceProfile.Update(insuranceUserProfile);
                }
            }
            await _repositoryWrapper.Save();
            return new ResponseMessage
            {
                Data = "Beneficiariary was deactivated successfully,beneficiary will remain active and would be moved to the inactive list at the end of current cycle.",
                Status = true,
                Message = "Beneficiary was deactivated successfully,beneficiary will remain active and would be moved to the inactive list at the end of current cycle."
            };
        }

        public async Task<ResponseMessage> DeactivateReferee(int userId, int refereeInsuranceId)
        {
            var payeeInsuranceProfile = await _repositoryWrapper.InsuranceProfile.GetByUserIdAsync(userId);
            var refereedInsuranceProfile = await _repositoryWrapper.InsuranceProfile.GetByIdAsync(refereeInsuranceId);
            if (refereedInsuranceProfile != null)
            {
                if (refereedInsuranceProfile.InsurancePayeeId == payeeInsuranceProfile.Id)
                {
                    var response = await ProcessCancelSubscription(refereedInsuranceProfile);
                    return response;
                }
                return new ResponseMessage { Message = "You cannot deactivate current profile" };
            }
            return new ResponseMessage { Message = "Referee profile as not found" };
        }

    }
}
