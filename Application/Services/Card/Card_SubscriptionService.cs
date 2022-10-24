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
        public async Task<ResponseMessage> ProcessFamilyCardTokenization(ChargeCardViewModel chargeCard, int id)
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
            insuranceUserProfile.PaymentMethod = PaymentMethod.Card.ToString();

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

        public async Task SendInsuranceListToInsuranceProvider(List<InsuranceUserProfile> insuranceUserProfiles)
        {
            foreach (var insuranceUserProfile in insuranceUserProfiles)
            {
                await _hmoIntegrationService.SendDetailsToInsuranceProvider(insuranceUserProfile);
            }
        }
                
        /// <summary>
        /// Switch payment method to card payment
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> SwitchToCardPayment(int userId)
        {
            var insuranceProfile = await _repositoryWrapper.InsuranceProfile.GetByUserIdAsync(userId);
            if (insuranceProfile != null)
            {
                var primaryCard = insuranceProfile.Cards.FirstOrDefault(x => x.Status == (int)DebitCard_StatusValue.primary);
                if (primaryCard == null)
                {
                    return new ResponseMessage { Message = "Kindly add a primary card, then start the reactivation process" , ResponseCode= 25 };
                }
                insuranceProfile.PaymentMethod = PaymentMethod.Card.ToString();
                _repositoryWrapper.InsuranceProfile.Update(insuranceProfile);
                await _repositoryWrapper.Save();
                return new ResponseMessage { Message = "Payment method was switched to card successfully", ResponseCode = 00, Status = true };
            }
            return new ResponseMessage { ResponseCode = 25, Message = "Profile not found" };
        }
    }
}
