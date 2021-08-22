
using Application.API_RequestModel.Paystack;
using Application.API_ResponseModel.Paystack;
using Application.DTO;
using Application.AuditAndReport.AuditLog;
using Application.Helpers;
using Application.Interfaces;
using Application.Services.Paystack;
using Application.ViewModels;
using Application.ViewModels.Paystack;
using AutoMapper;
using DataAccess.General.Interfaces;
using DataAccess.HealthInsured.Interfaces;
using Domain.Models;
using Domain.Models.Axa_Hygeia_Insurance;
using Hangfire;
using Hangfire.Server;
using HealthBanc.DTO.HealthInsured_AxaMansard;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.API_RequestModel.HealthInsured;
using Domain.Models.Axa.Hygeia_Insurance;
using Application.Services.Identity;
using DataAccess;
using Application.ViewModels.HealthInsured;
using Domain.Models.ReportAndLogs;
using System.Threading;
using Microsoft.Extensions.Logging;
using Application.HealthInsured_AxaMansard_Service.Insurance;
using Application.Services.HealthInsured;
using Application.Services.HealthInsured.Insurance;
using Application.API_ResponseModel.IBSResponse;
using Domain.Enums;

namespace Application.Services.HealthInsured_AxaMansard.Insurance
{
    public class TokenizationService
    {
        private readonly IMapper _mapper;
        private readonly PaystackService _paystackService;
        private readonly InsuranceService _insuranceSerivce;
        private readonly HMOIntegrationService _hmoIntegrationService;
        private readonly CorporateInsuranceService _corporateInsuranceService;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<TokenizationService> _logger;
        private readonly UtilityService _utilityService;
        private readonly IBSIntegrationService _iBSIntegrationService;
        private readonly FamilyInsuranceService _familyInsurance;
        private readonly IUniqueIdentifier _uniqueIdentifier;
        private readonly IRepositoryWrapper _repoWrapper;

        private SubscriptionDuration SubscriptionAccessor { get; }
        private HMOAccountDetails HMOAccountDetails { get; }


        public TokenizationService(IMapper mapper, PaystackService paystackService,InsuranceService insuranceSerivce,HMOIntegrationService hmoIntegrationService,CorporateInsuranceService corporateInsuranceService
            ,IOptions<SubscriptionDuration> subscriptionAccessor, IEmailSender emailSender,IRepositoryWrapper repoWrapper,ILogger<TokenizationService> logger,UtilityService utilityService
            , IOptions<HMOAccountDetails> hmoAccountAccessor, IBSIntegrationService iBSIntegrationService,FamilyInsuranceService familyInsurance, IUniqueIdentifier uniqueIdentifier)
        {
            _mapper = mapper;
            _paystackService = paystackService;
            _insuranceSerivce = insuranceSerivce;
            _hmoIntegrationService = hmoIntegrationService;
            _corporateInsuranceService = corporateInsuranceService;
            _emailSender = emailSender;
            _logger = logger;
            _utilityService = utilityService;
            _iBSIntegrationService = iBSIntegrationService;
            _familyInsurance = familyInsurance;
            _uniqueIdentifier = uniqueIdentifier;
            SubscriptionAccessor = subscriptionAccessor.Value;
            HMOAccountDetails = hmoAccountAccessor.Value;
            _repoWrapper = repoWrapper;
        }

        /// <summary>
        /// This Method is when user tokenize their card. For a user adding card, Just a fee of 50 naira is deducted to process their card ,get an authorization code to use in future payments
        /// </summary>
        /// <param name="chargeCard"></param>
        /// <param name="id"></param>
        /// <param name="ipAddress"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> TokenizeCard(ChargeCardViewModel chargeCard, int id)
        {
            var profileCompletion = await _insuranceSerivce.GetProfileCompletion(id,null);
            if (profileCompletion.Data.HealthInsuredPlan == HealthInsuredPlan.Individual)
            {
                return await ProcessIndividualCardTokenization(chargeCard, id);
            }
            else if(profileCompletion.Data.HealthInsuredPlan == HealthInsuredPlan.Corporate)
            {
                return await ProcessCorporateCardTokenization(chargeCard, id);
            }
            else if (profileCompletion.Data.HealthInsuredPlan == HealthInsuredPlan.Family)
            {
                return await ProcessFamilyCardTokenization(chargeCard, id);
            }
            return new ResponseMessage { Message = "User does not have a profile,kindly create your profile", Status = false };
        }

        #region "Individual Payment Flow After Card Tokenization"

        /// <summary>
        /// This is used to process card tokenization for indidvidual users.This method process the charge amount and process api Request that would be sent to paystack.
        /// </summary>
        /// <param name="chargeCard"></param>
        /// <param name="id"></param>
        /// <param name="ipAddress"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        private async Task<ResponseMessage> ProcessIndividualCardTokenization(ChargeCardViewModel chargeCard, int id)
        {
            var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByUserIdAsync(id);
            var checkprofileComplete = await _repoWrapper.InsuranceCompletionProfile.GetCompletionStateByUserId(id);
            if (checkprofileComplete.ProfileCompleted == true)
            {
                // remove empty space from the card.
                var cardNumber = chargeCard.card.number.Replace(" ", "");

                // Check if user has card that matches last four card digit
                var checkIfCardWasPreviouslyTokenized = await _repoWrapper.Card.CheckIfCardWasPreviouslyTokenized(id, cardNumber.Substring(cardNumber.Length - 4));
                if (checkIfCardWasPreviouslyTokenized != null) return new ResponseMessage { Message = "This card was previously tokenized" };

                var chargeCardRequest = _mapper.Map<API_RequestModel.Paystack.Card>(chargeCard.card);

                var card = new ChargeCard
                {
                    card = chargeCardRequest,
                    email = insuranceProfile.Email,
                    reference = Guid.NewGuid().ToString(),
                    pin = chargeCard.pin
                };

                card.amount = (50 * 100).ToString();

                // Create payment reference for the charge.
                var channel = insuranceProfile.InsuranceService == InsuranceProvider.Hygeia.ToString() ? PaymentReference_ChannelValue.healthinsured_hygeia.ToString()
                    : PaymentReference_ChannelValue.healthinsured_axamansard.ToString();

                var paymentReference = new PaymentReference(channel, card.reference, insuranceProfile.Id, null, null, insuranceProfile.UserId.Value
                , decimal.Parse("50"), PaymentReference_StatusValue.Pending.ToString());
                _repoWrapper.PaymentReference.Create(paymentReference);
                await _repoWrapper.Save();

                // Call Paystack service to charge user card
                var chargeCardResponse = await _paystackService.ChargeCard(card, id);

                // function to process response from paystack
                return await ProcessPaystackChargeCardResponse(chargeCardResponse, insuranceProfile, checkprofileComplete
                    , paymentReference, card.reference);
            }
            return new ResponseMessage { Message = "User has not been profiled,kindly create your profile", Status = false };
        }

        /// <summary>
        /// This is an overloaded method, which handle paystack charge card response for individual users.It is responsible for processing the logic and response
        /// if the charge was successful or not.
        /// It saves the debit card and calls the refund method if the charge was just a test charge.
        /// Also its responsible for calling the method which enroll users to either axamansard or hygeia when user tokenize card for the first time.
        /// </summary>
        /// <param name="chargeCardResponse"></param>
        /// <param name="insuranceUserProfile"></param>
        /// <param name="checkprofileComplete"></param>
        /// <param name="paymentReference"></param>
        /// <param name="cardReference"></param>
        /// <param name="ipAddress"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        private async Task<ResponseMessage> ProcessPaystackChargeCardResponse(TokenizationResponse chargeCardResponse, InsuranceUserProfile insuranceUserProfile,
            InsuranceCompletionProfile checkprofileComplete, PaymentReference paymentReference, string cardReference)
        {
            // if charge card was successfully
            if (chargeCardResponse.Status == true && chargeCardResponse.ResponseCode == 0)
            {
                //If card count is 0. it means there is no card available, so the card tokenised will
                //be the primary card so primary card status is set to 1 
                //else, card status is 0;
                var cardStatus = insuranceUserProfile.Cards.Count == 0 ? (int)DebitCard_StatusValue.primary : (int)DebitCard_StatusValue.secondary;

                var debitCard = new DebitCard(insuranceUserProfile.UserId.Value, insuranceUserProfile.Id, null, null, cardStatus, chargeCardResponse.LastDigit, chargeCardResponse.Type
                    , cardReference, chargeCardResponse.AuthorizationCode);
                _repoWrapper.Card.Create(debitCard);

                var activityLog = new ActivityLog(insuranceUserProfile.Id, null, null, "Debit Card Added", ServiceNames.HealthInsured.ToString());
                _repoWrapper.ActivityLog.Create(activityLog);

                checkprofileComplete.TokenizationCompleted = true;
                _repoWrapper.InsuranceProfile.Update(insuranceUserProfile);
                await _repoWrapper.Save();

                if (insuranceUserProfile.SubscriptionStatus is null)
                {
                    await SendDetailsToInsuranceProvider(insuranceUserProfile);
                    await Process_SuccessfulInsuranceIndividualPayment_FirstTimePayment(insuranceUserProfile);
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
            // Call method to process other transaction instance that is not successfull e.g Send_Otp,Send_Url,Failed.
            return await ProcessNotSuccessfulPaystackChargeCardResponse(paymentReference, chargeCardResponse);
        }

        private async Task Process_SuccessfulInsuranceIndividualPayment_FirstTimePayment(InsuranceUserProfile insuranceUserProfile)
        {
            insuranceUserProfile.EndActiveStatusDate = DateTime.Now.AddDays(SubscriptionAccessor.FreeTrialDayDuration);

            // Schedule debit email reminder for user 
            insuranceUserProfile.PendingEmailJobId = BackgroundJob.Schedule(() => _insuranceSerivce.SendEmailReminder(insuranceUserProfile.Email, insuranceUserProfile.Surname, null),
                    DateTime.Now.AddDays(SubscriptionAccessor.FreeTrialDayDuration).Subtract(new TimeSpan(3, 0, 0, 0)));

            //Schedule job to debit user every 28 days
            insuranceUserProfile.PendingJobId = ProcessScheduledPayment(insuranceUserProfile);

            var checkprofileComplete = await _repoWrapper.InsuranceCompletionProfile.GetCompletionStateByUserId(insuranceUserProfile.UserId.Value);
            checkprofileComplete.TokenizationCompleted = true;
            _repoWrapper.InsuranceCompletionProfile.Update(checkprofileComplete);

            _insuranceSerivce.SendSuccesfulSubscriptionMail(insuranceUserProfile.Email, insuranceUserProfile.Surname, insuranceUserProfile.TransId, insuranceUserProfile.CareProviderName,
                insuranceUserProfile.PlanCode);

            insuranceUserProfile.SubscriptionStatus = true;
            insuranceUserProfile.ActiveStatus = true;
            insuranceUserProfile.StartActiveStatusDate = DateTime.Now;

            _repoWrapper.InsuranceProfile.Update(insuranceUserProfile);

            //Create Audit thats user subscrption changed 
            var activityLog = new ActivityLog(insuranceUserProfile.Id, null, null, "Subscription was activated", ServiceNames.HealthInsured.ToString());
            _repoWrapper.ActivityLog.Create(activityLog);
            await _repoWrapper.Save();
        }
           
        /// <summary>
        /// Overloaded method to process individual scheduled payment.Create scheduled enrollment and scheduled payment data that is set to the processing stage.
        /// </summary>
        /// <param name="insuranceProfile"></param>
        /// <returns></returns>
        public string ProcessScheduledPayment(InsuranceUserProfile insuranceProfile)
        {
            var executionDate = insuranceProfile.EndActiveStatusDate;

            var userId = insuranceProfile.FamilyProfileId == null ? insuranceProfile.UserId.Value : 0;
 
            var jobId = BackgroundJob.Schedule(() => SchedulePaymentLogic(userId,
                    insuranceProfile.Id, null), executionDate);

            return jobId;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task SchedulePaymentLogic(int userId, int axamansardUserId, PerformContext context)
        {
            var activeCard = new DebitCard();
            var family = new FamilyProfile();
            var jobId = context.BackgroundJob.Id;
            var chageAuthorizationModel = new ChargeAuthorization();

            var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByIdAsync(axamansardUserId);
            if (!(insuranceProfile.FamilyProfileId is null))
            {
                family = await _repoWrapper.FamilyProfile.GetFamilyByFamilyId(insuranceProfile.FamilyProfileId.Value);
                activeCard = family.Cards.FirstOrDefault(x => x.Status == (int)DebitCard_StatusValue.primary);
                chageAuthorizationModel.email = family.Email;
            }
            else
            {
                activeCard = insuranceProfile.Cards.FirstOrDefault(x => x.Status == (int)DebitCard_StatusValue.primary);
                chageAuthorizationModel.email = insuranceProfile.Email;
            }


            chageAuthorizationModel.amount = (insuranceProfile.Premium * 100).ToString();
            chageAuthorizationModel.authorization_code = activeCard.Authorization_Code;

            var chargeAuthorization = await _paystackService.ChargeAuthorization(chageAuthorizationModel);
            if (chargeAuthorization.Status)
            {
                if (insuranceProfile.FamilyProfileId != null)
                {
                    await Process_SuccessfulInsuranceIndividualPayment_ScheduledPayment(insuranceProfile, family, chargeAuthorization.Reference);
                }
                else
                {
                    await Process_SuccessfulInsuranceIndividualPayment_ScheduledPayment(insuranceProfile, null, chargeAuthorization.Reference);
                }
            }
            else
            {
                var channel = insuranceProfile.InsuranceService == InsuranceProvider.Hygeia.ToString() ? PaymentReference_ChannelValue.healthinsured_hygeia.ToString()
                    : PaymentReference_ChannelValue.healthinsured_axamansard.ToString();

                if (!(insuranceProfile.FamilyProfileId is null))
                {
                    //_insuranceSerivce.SendEmailOnFailedDebit(insuranceProfile.Email, insuranceProfile.Surname, insuranceProfile.Premium.ToString());
                    var paymentReference = new PaymentReference(channel, chargeAuthorization.Reference, null, null, family.Id, family.UserId
                    , insuranceProfile.Premium, PaymentReference_StatusValue.Failed.ToString());
                    _repoWrapper.PaymentReference.Create(paymentReference);
                }
                else
                {
                    _insuranceSerivce.SendEmailOnFailedDebit(insuranceProfile.Email, insuranceProfile.Surname, insuranceProfile.Premium.ToString());
                    var paymentReference = new PaymentReference(channel, chargeAuthorization.Reference, insuranceProfile.Id, null, null, insuranceProfile.UserId.Value
                    , insuranceProfile.Premium, PaymentReference_StatusValue.Failed.ToString());
                    _repoWrapper.PaymentReference.Create(paymentReference);
                }

                insuranceProfile.PendingJobId = null; insuranceProfile.SubscriptionStatus = false;
                insuranceProfile.ActiveStatus = false;
                _repoWrapper.InsuranceProfile.Update(insuranceProfile);
                await _repoWrapper.InsuranceProfile.Save();
                BackgroundJob.Delete(jobId);
            }
            await Task.CompletedTask;
        }

        public async Task Process_SuccessfulInsuranceIndividualPayment_ScheduledPayment(InsuranceUserProfile insuranceUserProfile, FamilyProfile family, string reference)
        {
            if (insuranceUserProfile.InsuranceService.ToLower() != InsuranceProvider.Hygeia.ToString().ToLower() || insuranceUserProfile.InsuranceService == null)
            {
                await _insuranceSerivce.EnrollUserToAxamansardOnOnboarding(insuranceUserProfile);
            }

            insuranceUserProfile.EndActiveStatusDate = DateTime.Now.AddDays(SubscriptionAccessor.FreeTrialDayDuration);

            //Schedule job to debit user next 28 days
            insuranceUserProfile.PendingJobId = ProcessScheduledPayment(insuranceUserProfile);

            if (insuranceUserProfile.FamilyProfileId != null)
            {
                insuranceUserProfile.PendingEmailJobId = BackgroundJob.Schedule(() => _familyInsurance.SendPaymentReminder(family.Email, family.FullName
                        , insuranceUserProfile.Surname + " " + insuranceUserProfile.Othernames, null), insuranceUserProfile.EndActiveStatusDate.Subtract(new TimeSpan(3, 0, 0, 0)));
            }
            else
            {
                insuranceUserProfile.PendingEmailJobId = BackgroundJob.Schedule(() => SendEmailReminder(insuranceUserProfile.Email, insuranceUserProfile.Surname, null)
                , insuranceUserProfile.EndActiveStatusDate.Subtract(new TimeSpan(3, 0, 0, 0)));

            }
            // Schedule debit email reminder for user 
            insuranceUserProfile.SubscriptionStatus = true;
            insuranceUserProfile.ActiveStatus = true;
            insuranceUserProfile.StartActiveStatusDate = DateTime.Now;

            _repoWrapper.InsuranceProfile.Update(insuranceUserProfile);
            await _repoWrapper.Save();
        }

        /// <summary>
        /// method to cancel individual users subcription
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> CancelSubscription(int userId, string reason)
        {
            var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByUserIdAsync(userId);
            return await ProcessCancelSubscription(insuranceProfile);
        }

        /// <summary>
        /// Reactivate user Subscription with user primary card.
        /// If user Subscription status is false and cycle is active. Background job is scheduled to reactivate the user when the cysle ends.
        /// Else user subscription is reactivated immediately
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="ipAddress"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> ReactivateWithPresentPrimaryCard(int userId)
        {
            var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByUserIdAsync(userId);
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

        #endregion

        #region Family Payment Flow After Card Tokenization

            /// <summary>
            /// This is used to process card tokenization for family profiles.
            /// </summary>
            /// <param name="chargeCard"></param>
            /// <param name="id"></param>
            /// <returns></returns>
            private async Task<ResponseMessage> ProcessFamilyCardTokenization(ChargeCardViewModel chargeCard, int id)
            {
                var familyProfile = await _repoWrapper.FamilyProfile.GetExtendedFamilyDetails(id);
                if (familyProfile != null)
                {
                    if (familyProfile.EmailConfirmed)
                    {
                        // remove empty space from the card.
                        var cardNumber = chargeCard.card.number.Replace(" ", "");

                        // Check if user has card dat matches last four card digit
                        var checkIfCardWasPreviouslyTokenized = await _repoWrapper.Card.CheckIfCardWasPreviouslyTokenized(id, cardNumber.Substring(cardNumber.Length - 4));
                        if (checkIfCardWasPreviouslyTokenized != null) return new ResponseMessage { Message = "This card was previously tokenized" };

                        var chargeCardRequest = _mapper.Map<API_RequestModel.Paystack.Card>(chargeCard.card);

                        var card = new ChargeCard
                        {
                            card = chargeCardRequest,
                            email = familyProfile.Email,
                            reference = Guid.NewGuid().ToString(),
                            pin = chargeCard.pin
                        };

                        card.amount = (50 * 100).ToString();
                        var amount = decimal.Parse(card.amount) / 100;

                        // Create payment reference for the charge.
                        var channel = familyProfile.InsuranceService == InsuranceProvider.Hygeia.ToString() ? PaymentReference_ChannelValue.healthinsured_hygeia.ToString() : PaymentReference_ChannelValue.healthinsured_axamansard.ToString();
                        var paymentReference = new PaymentReference(channel, card.reference, null, null, familyProfile.Id, id, amount, PaymentReference_StatusValue.Pending.ToString());
                        _repoWrapper.PaymentReference.Create(paymentReference);
                        await _repoWrapper.Save();

                        // Paystack service to charge user card
                        var chargeCardResponse = await _paystackService.ChargeCard(card, id);

                        // function to process response from paystack
                        return await ProcessPaystackChargeCardResponse(chargeCardResponse, familyProfile, paymentReference, card.reference);
                    }
                    return new ResponseMessage { Message = "Kindly update your profile", Status = false };
                }
                return new ResponseMessage { Message = "User does not have a profile,kindly create your profile", Status = false };
            }

            public async Task<ResponseMessage> ProcessPaystackChargeCardResponse(TokenizationResponse chargeCardResponse, FamilyProfile familyProfile, PaymentReference paymentReference, string cardReference)
            {
                // if charge card was successfully
                if (chargeCardResponse.Status == true && chargeCardResponse.ResponseCode == 0)
                {
                    //If card count is 0. it means there is no card available, so the card tokenised will
                    //be the primary card so primary card status is set to 1 
                    //else, card status is 0;

                    var cardStatus = familyProfile.Cards.Count == 0 ? (int)DebitCard_StatusValue.primary : (int)DebitCard_StatusValue.secondary;

                    var debitCard = new DebitCard(familyProfile.UserId, null, null, familyProfile.Id, cardStatus, chargeCardResponse.LastDigit, chargeCardResponse.Type
                        , cardReference, chargeCardResponse.AuthorizationCode);
                    _repoWrapper.Card.Create(debitCard);

                    if (!familyProfile.TokenizationCompleted)
                    {
                        familyProfile.TokenizationCompleted = true;
                        _repoWrapper.FamilyProfile.Update(familyProfile);
                        await FamilyMembersActivation(familyProfile);   
                    }
                    var activityLog = new ActivityLog(null, null, familyProfile.Id, "Debit Card Added", ServiceNames.HealthInsured.ToString());
                    _repoWrapper.ActivityLog.Create(activityLog);
                    await _repoWrapper.Save();

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
            
            private async Task FamilyMembersActivation(FamilyProfile familyProfile)
            {
                var insuranceProfiles = familyProfile.InsuranceUserProfiles;
                foreach(var insuranceUserProfile in insuranceProfiles)
                {
                    insuranceUserProfile.EndActiveStatusDate = DateTime.Now.AddDays(SubscriptionAccessor.FreeTrialDayDuration);

                    // Schedule debit email reminder for user 
                    insuranceUserProfile.PendingEmailJobId = BackgroundJob.Schedule(() => _familyInsurance.SendPaymentReminder(familyProfile.Email, familyProfile.FullName,
                        $"{insuranceUserProfile.Othernames} {insuranceUserProfile.Surname}", null),
                           DateTime.Now.AddDays(SubscriptionAccessor.FreeTrialDayDuration).Subtract(new TimeSpan(3, 0, 0, 0)));

                    //Schedule job to debit user every 28 days
                    insuranceUserProfile.PendingJobId = ProcessScheduledPayment(insuranceUserProfile);

                    insuranceUserProfile.TransId = (insuranceUserProfile.InsuranceService == null | insuranceUserProfile.InsuranceService == InsuranceProvider.Axamansard.ToString())
                    ? _uniqueIdentifier.GetUniqueCode(10) : "";
                    insuranceUserProfile.SubscriptionStatus = true;
                    insuranceUserProfile.ActiveStatus = true;
                    insuranceUserProfile.StartActiveStatusDate = DateTime.Now;

                    _repoWrapper.InsuranceProfile.Update(insuranceUserProfile);
                    await _repoWrapper.Save();

                    await SendDetailsToInsuranceProvider(insuranceUserProfile);
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
                var familyProfile = await _repoWrapper.FamilyProfile.GetExtendedFamilyDetails(familyUserId);
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
                    // If  user is not in an active cycle
                    if (insuranceProfile.ActiveStatus == false ||insuranceProfile.ActiveStatus is null)
                    {
                        var response = await ProcessImmediateReactivationPayment(insuranceProfile, familyProfile.UserId, primaryCard.Authorization_Code);
                        if (response.Status)
                        {
                            // Schedule debit email reminder for user 
                            insuranceProfile.PendingEmailJobId = BackgroundJob.Schedule(() => _familyInsurance.SendPaymentReminder(familyProfile.Email, familyProfile.FullName
                                , insuranceProfile.Surname + " " + insuranceProfile.Othernames, null), insuranceProfile.EndActiveStatusDate.Subtract(new TimeSpan(3, 0, 0, 0)));
                            _repoWrapper.InsuranceProfile.Update(insuranceProfile);
                            await _repoWrapper.Save();
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
                            _repoWrapper.InsuranceProfile.Update(insuranceProfile);
                            await _repoWrapper.Save();
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

        #endregion

        #region "Corporate Payment Flow After Card Tokenization"

        /// <summary>
        /// This is used to process card tokenization for corporate org.This method process the charge amount and process api Request that would be
        /// sent to paystack. 
        /// </summary>
        /// <param name="chargeCard"></param>
        /// <param name="id"></param>
        /// <param name="ipAddress"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        private async Task<ResponseMessage> ProcessCorporateCardTokenization(ChargeCardViewModel chargeCard, int id)
        {
            var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(id);
            if (companyProfile != null)
            {
                if (companyProfile.EmailConfirmed)
                {
                    // remove empty space from the card.
                    var cardNumber = chargeCard.card.number.Replace(" ", "");

                    // Check if user has card dat matches last four card digit
                    var checkIfCardWasPreviouslyTokenized = await _repoWrapper.Card.CheckIfCardWasPreviouslyTokenized(id, cardNumber.Substring(cardNumber.Length - 4));
                    if (checkIfCardWasPreviouslyTokenized != null) return new ResponseMessage { Message = "This card was previously tokenized" };

                    var chargeCardRequest = _mapper.Map<API_RequestModel.Paystack.Card>(chargeCard.card);

                    var card = new ChargeCard
                    {
                        card = chargeCardRequest,
                        email = companyProfile.CompanyEmail,
                        reference = Guid.NewGuid().ToString(),
                        pin = chargeCard.pin
                    };

                    // When user is tokenizing card
                    if (!companyProfile.TokenizationCompleted)
                    {
                        var beneficiaryReviews = await _repoWrapper.BeneficiaryReview.QueryableBeneficiaryReviews(id);
                        var totalAmount = beneficiaryReviews.Where(x => !x.IsRemove).Select(x => x.Amount).Sum();
                        if (totalAmount == 0)
                        {
                            return new ResponseMessage { Message = "Kindly add at least one beneficiary" };
                        }
                        card.amount = (totalAmount * 100).ToString();
                    }
                    else
                    {
                        // When user is adding card
                        card.amount = (100 * 50).ToString();
                    }
                    var amount = decimal.Parse(card.amount) / 100;
                    // Create payment reference for the charge.
                    var channel = companyProfile.InsuranceService == InsuranceProvider.Hygeia.ToString() ? PaymentReference_ChannelValue.healthinsured_hygeia.ToString() : PaymentReference_ChannelValue.healthinsured_axamansard.ToString();
                    var paymentReference = new PaymentReference(channel, card.reference, null, companyProfile.Id, null, id, amount, PaymentReference_StatusValue.Pending.ToString());
                    _repoWrapper.PaymentReference.Create(paymentReference);
                    await _repoWrapper.Save();

                    // Paystack service to charge user card
                    var chargeCardResponse = await _paystackService.ChargeCard(card, id);

                    // function to process response from paystack
                    return await ProcessPaystackChargeCardResponse(chargeCardResponse, companyProfile, paymentReference, card.reference);
                }
                return new ResponseMessage { Message = "Kindly update your profile", Status = false };
            }
            return new ResponseMessage { Message = "User does not have a profile,kindly create your profile", Status = false };
        }

        /// <summary>
        /// This is an overloaded method, which handle paystack charge card response for corporate org.It is responsible for processing the logic and response
        /// if the charge was successful or not.
        /// </summary>
        /// <param name="chargeCardResponse"></param>
        /// <param name="companyProfile"></param>
        /// <param name="paymentReference"></param>
        /// <param name="cardReference"></param>
        /// <param name="ipAddress"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> ProcessPaystackChargeCardResponse(TokenizationResponse chargeCardResponse, CompanyProfile companyProfile,
           PaymentReference paymentReference, string cardReference)
        {
            // if charge card was successfully
            if (chargeCardResponse.Status == true && chargeCardResponse.ResponseCode == 0)
            {
                //If card count is 0. it means there is no card available, so the card tokenised will
                //be the primary card so primary card status is set to 1 
                //else, card status is 0;

                var cardStatus = companyProfile.Cards.Count == 0 ? (int)DebitCard_StatusValue.primary : (int)DebitCard_StatusValue.secondary;

                var debitCard = new DebitCard(companyProfile.UserId, null, companyProfile.Id, null, cardStatus, chargeCardResponse.LastDigit, chargeCardResponse.Type
                    , cardReference, chargeCardResponse.AuthorizationCode);
                _repoWrapper.Card.Create(debitCard);

                if (!companyProfile.TokenizationCompleted)
                {
                    companyProfile.TokenizationCompleted = true;
                    _repoWrapper.CompanyProfile.Update(companyProfile);

                }
                var activityLog = new ActivityLog(null, companyProfile.Id, null, "Debit Card Added", ServiceNames.HealthInsured.ToString());
                _repoWrapper.ActivityLog.Create(activityLog);
                await _repoWrapper.Save();

                /// We sleep the thread for 10 seconds so we can process the paystack webhook  <see cref="ProcessPaystackWebHook(string, string, string, string, string, string, string)"/>
                Thread.Sleep(15000);

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
        /// Overloaded method to process corporate scheduled payment.Create scheduled enrollment and scheduled payment data that is set to the processing stage.
        /// </summary>
        /// <param name="insuranceProfile"></param>
        /// <returns></returns>
        public string ProcessScheduledPayment(CompanyProfile companyProfile)
        {
            var executionDate = companyProfile.NextPaymentDate.Value;

            var jobId = BackgroundJob.Schedule(() => SchedulePaymentLogic(companyProfile.UserId, null), executionDate);

            return jobId;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task SchedulePaymentLogic(int userId, PerformContext context)
        {
            var jobId = context.BackgroundJob.Id;
            var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyInsuranceUserProfilesByUserId(userId);
            var activeCard = companyProfile.Cards.FirstOrDefault(x => x.Status == (int)DebitCard_StatusValue.primary);

            var premiumFee = companyProfile.InsuranceUserProfiles.Where(x => x.CompanySubscribedStatus == InsuranceProfile_CompanySubStatusValue.Active.ToString() ||
            x.CompanySubscribedStatus == InsuranceProfile_CompanySubStatusValue.Pending.ToString()).Select(x => x.Premium).Sum();

            // set the next repayment date at first trial i.e when there is no failed scheduled payment attempt !!!!!
            if (companyProfile.FailedScheduledPaymentRetry is null)
            {
                companyProfile.NextPaymentDate = DateTime.Now.AddDays(SubscriptionAccessor.FreeTrialDayDuration);
            }

            // If there is active or pending users
            if (premiumFee >= 1000)
            {
                //Get a charge authorization model to use in scheduled payment background process.
                var chageAuthorizationModel = new ChargeAuthorization()
                {
                    email = companyProfile.CompanyEmail,
                    amount = (premiumFee * 100).ToString(),
                    authorization_code = activeCard.Authorization_Code
                };

                var chargeAuthorization = await _paystackService.ChargeAuthorization(chageAuthorizationModel);
                // if debit was successfully
                if (chargeAuthorization.Status)
                {

                    //Task to Enroll pending users to HMO
                    await _corporateInsuranceService.OnboardCompanyUsersToHMO(companyProfile.UserId, InsuranceProfile_CompanySubStatusValue.Pending.ToString(), companyProfile.NextPaymentDate.Value,
                        companyProfile.InsuranceService);

                    //Background task to schedule debit at the end of next cycle
                    companyProfile.PendingJobId = ProcessScheduledPayment(companyProfile);

                    companyProfile.PendingEmailJobId = BackgroundJob.Schedule(() => _insuranceSerivce.SendEmailReminder(companyProfile.CompanyEmail, companyProfile.CompanyName, null),
                          DateTime.Now.AddDays(SubscriptionAccessor.FreeTrialDayDuration).Subtract(new TimeSpan(3, 0, 0, 0)));

                    companyProfile.FailedScheduledPaymentRetry = null;
                    companyProfile.TokenizationCompleted = true;

                    _repoWrapper.CompanyProfile.Update(companyProfile);

                    var companyBeneficiaires = companyProfile.InsuranceUserProfiles.Where(x => x.CompanySubscribedStatus == InsuranceProfile_CompanySubStatusValue.Active.ToString()).ToList();
                    foreach (var item in companyBeneficiaires)
                    {
                        item.StartActiveStatusDate = DateTime.Now;
                        item.EndActiveStatusDate = DateTime.Now.AddDays(SubscriptionAccessor.FreeTrialDayDuration);
                    }
                    _repoWrapper.InsuranceProfile.UpdateRange(companyBeneficiaires);

                    await _repoWrapper.Save();
                }
                else
                {
                    var channel = PaymentReference_ChannelValue.healthinsured_hygeia.ToString();

                    var paymentReference = new PaymentReference(channel, chargeAuthorization.Reference, null, companyProfile.Id, null, companyProfile.UserId
                        , premiumFee, PaymentReference_StatusValue.Failed.ToString());
                    _repoWrapper.PaymentReference.Create(paymentReference);

                    // If debit retries is less than 11 times Schedule debit for next four hours
                    if (companyProfile.FailedScheduledPaymentRetry is null || companyProfile.FailedScheduledPaymentRetry < 11)
                    {
                        companyProfile.PendingEmailJobId = null;

                        /// Schedule debit in the next 4 hours
                        companyProfile.PendingJobId = BackgroundJob.Schedule(() => SchedulePaymentLogic(companyProfile.UserId, null), DateTime.Now.AddDays(SubscriptionAccessor.FailedDebitRetrialDuration));

                        companyProfile.FailedScheduledPaymentRetry = companyProfile.FailedScheduledPaymentRetry.HasValue ? companyProfile.FailedScheduledPaymentRetry += 1 : 1;
                        _repoWrapper.CompanyProfile.Update(companyProfile);
                        await _repoWrapper.Save();

                        var leftProbationHours = (48 - (companyProfile.FailedScheduledPaymentRetry * 4));
                        var probationendDate = DateTime.Now.AddHours(Double.Parse(leftProbationHours.ToString()));
                        _insuranceSerivce.SendCompanyEmailOnFailedDebit(companyProfile.CompanyEmail, companyProfile.CompanyName, premiumFee.ToString()
                            , probationendDate.ToLongDateString());
                    }
                    else
                    {
                        // Set debit to the next insurance cycle after payment probation hours (48 hr) or maximum retries is exceded & deactivate all beneficiaries
                        companyProfile.PendingEmailJobId = null;
                        companyProfile.PendingJobId = ProcessScheduledPayment(companyProfile);
                        companyProfile.FailedScheduledPaymentRetry = null;
                        _repoWrapper.CompanyProfile.Update(companyProfile);
                        await _repoWrapper.Save();

                        await DeactivateAllCompanybeneficiaries(companyProfile.UserId);

                        _insuranceSerivce.SendCompanyDeactivationMail(companyProfile.CompanyEmail, companyProfile.CompanyName, premiumFee.ToString());
                    }
                }
            }
            else
            {
                var newJobId2 = ProcessScheduledPayment(companyProfile);
                companyProfile.PendingJobId = newJobId2;
                companyProfile.PendingEmailJobId = null;
                companyProfile.FailedScheduledPaymentRetry = null;
                _repoWrapper.CompanyProfile.Update(companyProfile);
                await _repoWrapper.Save();
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// method to cancel corporate insurance users subcription
        /// </summary>
        /// <param name="beneficiaryListViewModel"></param>
        /// <param name="companyUserId"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> DeactivateCompanyBeneficiaries(BeneficiaryListViewModel beneficiaryListViewModel, int companyUserId)
        {
            var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(companyUserId);
            foreach (var item in beneficiaryListViewModel.Emails)
            {
                var insuranceUserProfile = await _repoWrapper.InsuranceProfile.GetByEmail(item);
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
                        BackgroundJob.Schedule(() => ProcessUserActiveStatusCancellation(insuranceUserProfile.Id,null), companyProfile.NextPaymentDate.Value);
                    }
                    _repoWrapper.InsuranceProfile.Update(insuranceUserProfile);
                }
            }
            await _repoWrapper.Save();
            return new ResponseMessage
            {
                Data = "Beneficiariary was deactivated successfully,beneficiary will remain active and would be moved to the inactive list at the end of current cycle.",
                Status = true,
                Message = "Beneficiary was deactivated successfully,beneficiary will remain active and would be moved to the inactive list at the end of current cycle."
            };
        }

        /// <summary>
        /// Deactivate all company insurance users
        /// </summary>
        /// <param name="companyUserId"></param>
        /// <returns></returns>
        public async Task DeactivateAllCompanybeneficiaries(int companyUserId)
        {
            var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyInsuranceUserProfilesByUserId(companyUserId);
            foreach (var item in companyProfile.InsuranceUserProfiles)
            {
                await _hmoIntegrationService.HygeiaDeactivateUser(item.TransId);
                item.ActiveStatus = false;
                item.CompanySubscribedStatus = InsuranceProfile_CompanySubStatusValue.Inactive.ToString();
                item.SubscriptionStatus = false;
                _repoWrapper.InsuranceProfile.Update(item);
                await _repoWrapper.Save();
            }
            await Task.CompletedTask;
        }

        #endregion

        #region Payment To HealthInsurance Providers Obsolute in A month time 10/12/2021

        [AutomaticRetry(Attempts = 0)]
        public async Task ProcessHygeiaHMOPayment(string fromAccount, string toAccount, decimal? amount, string getJobId, PerformContext context)
        {
            await _utilityService.ProcessHygeiaHMOPayment(fromAccount, toAccount, amount, getJobId, context);
            await Task.CompletedTask;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task ProcessAxamansardHMOPayment(string fromAccount, string toAccount, decimal? amount, string getJobId, PerformContext context)
        {
            await _utilityService.ProcessAxamansardHMOPayment(fromAccount, toAccount, amount, getJobId, context);
            await Task.CompletedTask;
        }

        #endregion

        #region Card Management

            /// <summary>
            /// Service to change primary card
            /// </summary>
            /// <param name="newCardId"></param>
            /// <param name="userId"></param>
            /// <param name="ipAddress"></param>
            /// <param name="device"></param>
            /// <returns></returns>
            public async Task<ResponseMessage> ChangePrimaryCard(int newCardId, int userId)
            {
                var presentPrimaryCard = await _repoWrapper.Card.GetPrimaryCard(userId);
                if (presentPrimaryCard == null)
                {
                    return new ResponseMessage { Message = "You dont have a debit card, Kindly add one" };
                }

                var newPrimaryCard = await _repoWrapper.Card.GetCardByIdAsync(newCardId, userId);
                if (newPrimaryCard == null)
                {
                    return new ResponseMessage { Message = "Card was not found", ResponseCode = 12 };
                }
                if (newPrimaryCard.Status == (int)DebitCard_StatusValue.primary)
                {
                    return new ResponseMessage { Message = "This card is presently the primary card" };
                }
                presentPrimaryCard.Status = (int)DebitCard_StatusValue.secondary;
                _repoWrapper.Card.Update(presentPrimaryCard);
                newPrimaryCard.Status = (int)DebitCard_StatusValue.primary;
                _repoWrapper.Card.Update(newPrimaryCard);

                if (newPrimaryCard.InsuranceUserProfileId != null)
                {
                    var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByUserIdAsync(userId);
                    var activityLog = new ActivityLog(insuranceProfile.Id, null, null, "Change Primary Card", ServiceNames.HealthInsured.ToString());
                    _repoWrapper.ActivityLog.Create(activityLog);
                }
                else if (newPrimaryCard.CompanyProfileId != null)
                {
                    var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(userId);
                    var activityLog = new ActivityLog(null, companyProfile.Id, null, "Change Primary Card", ServiceNames.HealthInsured.ToString());
                    _repoWrapper.ActivityLog.Create(activityLog);
                }
                else
                {
                    var familyProfile = await _repoWrapper.FamilyProfile.GetByUserId(userId);
                    var activityLog = new ActivityLog(null, null, familyProfile.Id, "Change Primary Card", ServiceNames.HealthInsured.ToString());
                    _repoWrapper.ActivityLog.Create(activityLog);
                }

                await _repoWrapper.Save();
                return new ResponseMessage { Message = "Primary card was changed successfully", Status = true };
            }

            /// <summary>
            /// Service to delete debit card
            /// </summary>
            /// <param name="cardId"></param>
            /// <param name="userId"></param>
            /// <param name="ipAddress"></param>
            /// <param name="device"></param>
            /// <returns></returns>
            public async Task<ResponseMessage> DeleteCard(int cardId, int userId)
            {
                var card = await _repoWrapper.Card.GetCardByIdAsync(cardId, userId);
                bool isCardDeletable = false;
                if (card == null) return new ResponseMessage { Message = "Card  was not found", ResponseCode = 12 };

                if (card.InsuranceUserProfileId != null)
                {
                    var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByUserIdAsync(userId);
                    if (card.Status == (int)DebitCard_StatusValue.primary && insuranceProfile.SubscriptionStatus == true)
                    {
                        isCardDeletable = true;
                        var activityLog = new ActivityLog(insuranceProfile.Id, null, null, "Debit Card Was Removed", ServiceNames.HealthInsured.ToString());
                        _repoWrapper.ActivityLog.Create(activityLog);
                    }
                }
                else if (card.CompanyProfileId != null)
                {
                    var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(userId);
                    if (card.Status == (int)DebitCard_StatusValue.primary)
                    {
                        isCardDeletable = true;
                        var activityLog = new ActivityLog(null, companyProfile.Id, null, "Debit Card Was Removed", ServiceNames.HealthInsured.ToString());
                        _repoWrapper.ActivityLog.Create(activityLog);
                    }
                }
                else
                {
                    var familyProfile = await _repoWrapper.FamilyProfile.GetByUserId(userId);
                    if (card.Status == (int)DebitCard_StatusValue.primary)
                    {
                        isCardDeletable = true;
                        var activityLog = new ActivityLog(null, null, familyProfile.Id, "Debit Card Was Removed", ServiceNames.HealthInsured.ToString());
                        _repoWrapper.ActivityLog.Create(activityLog);
                    }
                }

                if (isCardDeletable)
                {
                    _repoWrapper.Card.Delete(card);
                    await _repoWrapper.Save();
                    return new ResponseMessage { Message = "Card was deleted successfully", Status = true };
                }
                else
                {
                    return new ResponseMessage
                    {
                        Message = "Kindly set a new card as" +
                      "primary card to delete present primary card"
                    };
                }
            }

            /// <summary>
            /// Fetch debit card
            /// </summary>
            /// <param name="userId"></param>
            /// <returns></returns>
            public async Task<ResponseMessage> GetCards(int userId)
        {
            var profileCompletion = await _insuranceSerivce.GetProfileCompletion(userId,null);
            List<DebitCard> debitCard = new List<DebitCard>();
            if (profileCompletion.Data.HealthInsuredPlan == HealthInsuredPlan.Individual)
            {
                var individualInsuranceUser = await _repoWrapper.InsuranceProfile.GetByUserIdAsync(userId);
                debitCard = individualInsuranceUser.Cards;
            }
            else if (profileCompletion.Data.HealthInsuredPlan == HealthInsuredPlan.Corporate)
            {
                var coporateInsuranceUser = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(userId);
                debitCard = coporateInsuranceUser.Cards;
            }
            else
            {
                var familyProfile = await _repoWrapper.FamilyProfile.GetByUserId(userId);
                debitCard = familyProfile.Cards;
            }
            if (debitCard.Count > 0)
            {
                var cardDTO = _mapper.Map<List<DebitCard>, List<CardDTO>>(debitCard);
                return new ResponseMessage { Data = cardDTO, Status = true, Message = "Card was fetchd successfully" };
            }
            var emptyCardDTO = new List<CardDTO>();
            return new ResponseMessage { Data = emptyCardDTO, Message = "User has no card, Kindly add a card", Status = true };
        }

        #endregion

        #region

        public async Task<ResponseMessage> DeactivateReferee(int userId, int refereeInsuranceId)
        {
            var payeeInsuranceProfile = await _repoWrapper.InsuranceProfile.GetByUserIdAsync(userId);
            var refereedInsuranceProfile = await _repoWrapper.InsuranceProfile.GetByIdAsync(refereeInsuranceId);
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

        public async Task<ResponseMessage> ActivateRefereeWithPrimaryCard(int userId, int refereeInsuranceId)
        {
            var payeeInsuranceProfile = await _repoWrapper.InsuranceProfile.GetByUserIdAsync(userId);
            var refereedInsuranceProfile = await _repoWrapper.InsuranceProfile.GetByIdAsync(refereeInsuranceId);
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
                        // Schedule debit email reminder for user 
                        //insuranceProfile.PendingEmailJobId = BackgroundJob.Schedule(() => _familyInsurance.SendPaymentReminder(familyProfile.Email, familyProfile.FullName
                        //    , insuranceProfile.Surname + " " + insuranceProfile.Othernames, null), insuranceProfile.EndActiveStatusDate.Subtract(new TimeSpan(3, 0, 0, 0)));
                        _repoWrapper.InsuranceProfile.Update(refereedInsuranceProfile);
                        await _repoWrapper.Save();
                    }
                    return response;
                }
                // If user is in an active cycle
                else
                {
                    var response = await ProcessScheduledReactivationFlow(refereedInsuranceProfile, refereedInsuranceProfile.EndActiveStatusDate);
                    if (response.Status)
                    {
                        // Schedule debit email reminder for user 
                        //insuranceProfile.PendingEmailJobId = BackgroundJob.Schedule(() => _familyInsurance.SendPaymentReminder(familyProfile.Email, familyProfile.FullName
                        //    , insuranceProfile.Surname + " " + insuranceProfile.Othernames, null), insuranceProfile.EndActiveStatusDate.Subtract(new TimeSpan(3, 0, 0, 0)));
                        _repoWrapper.InsuranceProfile.Update(refereedInsuranceProfile);
                        await _repoWrapper.Save();
                    }
                    return response;
                }
            }
            return new ResponseMessage { Message = "Insurance profile does not exist under your family profile!" };
        }
        #endregion
        public async Task<ResponseMessage> ProcessNotSuccessfulPaystackChargeCardResponse(PaymentReference paymentReference,TokenizationResponse chargeCardResponse)
        {
            // If charge card response request for OTP
            if (chargeCardResponse.Status == true && chargeCardResponse.ResponseCode == 12)
            {
                paymentReference.Status = PaymentReference_StatusValue.Send_Otp.ToString();
                _repoWrapper.PaymentReference.Update(paymentReference);
                await _repoWrapper.Save();
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
                _repoWrapper.PaymentReference.Update(paymentReference);
                await _repoWrapper.Save();
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
            _repoWrapper.PaymentReference.Update(paymentReference);
            await _repoWrapper.Save();
            return new ResponseMessage
            {
                Data = chargeCardResponse.Data,
                Message = chargeCardResponse.Message,
                Status = chargeCardResponse.Status,
                ResponseCode = chargeCardResponse.ResponseCode
            };
        }
        public async Task<ResponseMessage> SubmitOtp(SetOtpViewModel otpViewModel, int id)
        {
            var profileCompletion = await _insuranceSerivce.GetProfileCompletion(id,null);
            var paymentReference = await _repoWrapper.PaymentReference.GetByReference(otpViewModel.reference);
            if (profileCompletion.Data.HealthInsuredPlan == HealthInsuredPlan.Individual)
            {
                var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByUserIdAsync(id);
                var checkprofileComplete = await _repoWrapper.InsuranceCompletionProfile.GetCompletionStateByUserId(id);

                var chargeCardResponse = await _paystackService.SendOtp(otpViewModel.otp, otpViewModel.reference, insuranceProfile.PhoneNumber
                       , insuranceProfile.DateOfBirth, otpViewModel.pin);
                return await ProcessPaystackChargeCardResponse(chargeCardResponse, insuranceProfile, checkprofileComplete,
                    paymentReference, otpViewModel.reference);
            }
            else if (profileCompletion.Data.HealthInsuredPlan == HealthInsuredPlan.Corporate)
            {
                var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(id);
                var chargeCardResponse = await _paystackService.SendOtp(otpViewModel.otp, otpViewModel.reference, companyProfile.PhoneNumber
                       , DateTime.Now, otpViewModel.pin);
                return await ProcessPaystackChargeCardResponse(chargeCardResponse, companyProfile, paymentReference, otpViewModel.reference);
            }
            else if (profileCompletion.Data.HealthInsuredPlan == HealthInsuredPlan.Family)
            {
                var familyProfile = await _repoWrapper.FamilyProfile.GetByUserId(id);
                var chargeCardResponse = await _paystackService.SendOtp(otpViewModel.otp, otpViewModel.reference, familyProfile.PhoneNumber
                       , DateTime.Now, otpViewModel.pin);
                return await ProcessPaystackChargeCardResponse(chargeCardResponse, familyProfile, paymentReference, otpViewModel.reference);
            }
            return new ResponseMessage { Message = "User does not have a profile,kindly create your profile", Status = false };
        }
        private async Task<ResponseMessage> ProcessCancelSubscription(InsuranceUserProfile insuranceProfile)
        {
            if (insuranceProfile.SubscriptionStatus == false || insuranceProfile.SubscriptionStatus is null)
            {
                return new ResponseMessage { Message = "Insurance Profile has no subscription", Status = false };
            }
            if (insuranceProfile.TransId == null || insuranceProfile.TransId == "" || insuranceProfile.TransId == "Pending")
            {
                return new ResponseMessage { Message = "Subscription can only be cancelled after enrollee number has been processed. this should take less than 2 working days" };
            }

            var scheduledJobId = insuranceProfile.PendingJobId;

            BackgroundJob.Delete(scheduledJobId);
            if (insuranceProfile.PendingEmailJobId != null)
            {
                BackgroundJob.Delete(insuranceProfile.PendingEmailJobId);
            }

            var daysToCancelUserActivityStatus = insuranceProfile.EndActiveStatusDate;
            // Schedule task to render user status inactive when cycle ends
            var jobId = BackgroundJob.Schedule(() => ProcessUserActiveStatusCancellation(insuranceProfile.Id,null), daysToCancelUserActivityStatus);

            insuranceProfile.PendingJobId = jobId;
            insuranceProfile.PendingEmailJobId = null;
            insuranceProfile.SubscriptionStatus = false;
            _repoWrapper.InsuranceProfile.Update(insuranceProfile);

            var activityLog = new ActivityLog(insuranceProfile.Id, null, null, "Subscription Was Cancelled Successfully", ServiceNames.HealthInsured.ToString());
            _repoWrapper.ActivityLog.Create(activityLog);

            await _repoWrapper.Save();
            var profileDTO = _mapper.Map<IndividualProfileDTO>(insuranceProfile);
            return new ResponseMessage
            {
                Data = profileDTO,
                Message = "Subscription was canceled successfully.However profile will remain active till " +
                "insurance cycle ends.",
                Status = true
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task ProcessUserActiveStatusCancellation(int userId)
        {
            var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByUserIdAsync(userId);
            if (insuranceProfile.InsuranceService.ToLower() == InsuranceProvider.Hygeia.ToString().ToLower())
            {
                await _hmoIntegrationService.HygeiaDeactivateUser(insuranceProfile.TransId);
            }
            insuranceProfile.ActiveStatus = false;
            _repoWrapper.InsuranceProfile.Update(insuranceProfile);
            await _repoWrapper.Save();
            var insuranceProfile2 = await _repoWrapper.InsuranceProfile.GetByUserIdAsync(userId);
            if (insuranceProfile2.ActiveStatus is true)
            {
                insuranceProfile.ActiveStatus = false;
                _repoWrapper.InsuranceProfile.Update(insuranceProfile);
                await _repoWrapper.Save();
            }
            await Task.CompletedTask;
        }

        public async Task ProcessUserActiveStatusCancellation(int insuranceProfileId,PerformContext performContext)
        {
            var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByIdAsync(insuranceProfileId);
            if (insuranceProfile.InsuranceService.ToLower() == InsuranceProvider.Hygeia.ToString().ToLower())
            {
                await _hmoIntegrationService.HygeiaDeactivateUser(insuranceProfile.TransId);
            }
            insuranceProfile.ActiveStatus = false;
            _repoWrapper.InsuranceProfile.Update(insuranceProfile);
            await _repoWrapper.Save();
            var insuranceProfile2 = await _repoWrapper.InsuranceProfile.GetByIdAsync(insuranceProfileId);
            if (insuranceProfile2.ActiveStatus is true)
            {
                insuranceProfile.ActiveStatus = false;
                _repoWrapper.InsuranceProfile.Update(insuranceProfile);
                await _repoWrapper.Save();
            }
            await Task.CompletedTask;
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
        private async Task<ResponseMessage> ProcessScheduledReactivationFlow(InsuranceUserProfile insuranceProfile,DateTime? reactivationTime)
        {  
            // If user is still in active cycle and want to reactivate, that means user must have a background job scheduled to render user INACTIVE
            // at the end of cycle.
            // To resolve this, we delete background task scheduled to render user inactive at the end of current cycle          
            BackgroundJob.Delete(insuranceProfile.PendingJobId);

            // Task to process scheduled payment at end of current active cycle
            var jobId = ProcessScheduledPayment(insuranceProfile);

            // Schedule debit email reminder for user 
            if(insuranceProfile.FamilyProfileId is null)
            {
                insuranceProfile.PendingEmailJobId = BackgroundJob.Schedule(() => SendEmailReminder(insuranceProfile.Email, insuranceProfile.Surname, null), insuranceProfile.EndActiveStatusDate.Subtract(new TimeSpan(3, 0, 0, 0)));
            }

            insuranceProfile.SubscriptionStatus = true;
            insuranceProfile.ActiveStatus = true;
            insuranceProfile.PendingJobId = jobId;
            _repoWrapper.InsuranceProfile.Update(insuranceProfile);
            await _repoWrapper.Save();
            return new ResponseMessage {Status = true,Message= "Activation was successful.You will be debited at the end of " +
                "active cycle" };
        }

        /// <summary>
        /// Func to process immediate user reactivation
        /// </summary>
        /// <param name="userAxamansardProfile"></param>
        /// <param name="authorization_Code"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> ProcessImmediateReactivationPayment(InsuranceUserProfile insuranceProfile,int userId, string authorization_Code)
        {
            var chageAuthorizationModel = new ChargeAuthorization()
            {
                email = insuranceProfile.Email,
                amount = (insuranceProfile.Premium * 100).ToString(),
                authorization_code = authorization_Code
            };

            if(insuranceProfile.FamilyProfileId != null)
            {
                var family = await _repoWrapper.FamilyProfile.GetFamilyByFamilyId(insuranceProfile.FamilyProfileId.Value);
                chageAuthorizationModel.email = family.Email;
            }

            // Call Paystack service. Debit user using card authorization code
            var chargeAuthorization = await _paystackService.ChargeAuthorization(chageAuthorizationModel);
            if (chargeAuthorization.Status)
            {
                await SendDetailsToInsuranceProvider(insuranceProfile);
                await Process_SuccessfulInsuranceIndividualPayment_ImmediateReactivationPayment(insuranceProfile);
                return new ResponseMessage { Message = "Reactivation was successful." , Status = true, ResponseCode = chargeAuthorization.ResponseCode };
            }
            // Setfailed payment reference for transacation;
            var channel = insuranceProfile.InsuranceService == InsuranceProvider.Hygeia.ToString() ? PaymentReference_ChannelValue.healthinsured_hygeia.ToString() 
                : PaymentReference_ChannelValue.healthinsured_axamansard.ToString();

            if(insuranceProfile.FamilyProfileId is null)
            {
                var paymentReference = new PaymentReference(channel, chargeAuthorization.Reference, insuranceProfile.Id, null, null, userId
                   , insuranceProfile.Premium, PaymentReference_StatusValue.Failed.ToString());
                _repoWrapper.PaymentReference.Create(paymentReference);
            }
            else
            {
                var paymentReference = new PaymentReference(channel, chargeAuthorization.Reference, null, null, insuranceProfile.FamilyProfileId, userId
                  , insuranceProfile.Premium, PaymentReference_StatusValue.Failed.ToString());
                _repoWrapper.PaymentReference.Create(paymentReference);
            }
            
            await _repoWrapper.Save();

            return new ResponseMessage { Message = chargeAuthorization.Message, Status =false, ResponseCode = chargeAuthorization.ResponseCode };
        }

        private async Task Process_SuccessfulInsuranceIndividualPayment_ImmediateReactivationPayment(InsuranceUserProfile insuranceUserProfile)
        {
            insuranceUserProfile.EndActiveStatusDate = DateTime.Now.AddDays(SubscriptionAccessor.FreeTrialDayDuration);

            //Schedule job to debit user next 28 days
            insuranceUserProfile.PendingJobId = ProcessScheduledPayment(insuranceUserProfile);

            // Schedule debit email reminder for user 
            if(insuranceUserProfile.FamilyProfileId != null)
            {
                var family = await _repoWrapper.FamilyProfile.GetFamilyByFamilyId(insuranceUserProfile.FamilyProfileId.Value);
                insuranceUserProfile.PendingEmailJobId = BackgroundJob.Schedule(() => _familyInsurance.SendPaymentReminder(family.Email, family.FullName
                       , insuranceUserProfile.Surname + " " + insuranceUserProfile.Othernames, null), insuranceUserProfile.EndActiveStatusDate.Subtract(new TimeSpan(3, 0, 0, 0)));
            }
            else
            {
                insuranceUserProfile.PendingEmailJobId = BackgroundJob.Schedule(() => SendEmailReminder(insuranceUserProfile.Email, insuranceUserProfile.Surname, null)
                , insuranceUserProfile.EndActiveStatusDate.Subtract(new TimeSpan(3, 0, 0, 0)));
            }

            insuranceUserProfile.SubscriptionStatus = true;
            insuranceUserProfile.ActiveStatus = true;
            insuranceUserProfile.StartActiveStatusDate = DateTime.Now;
            _repoWrapper.InsuranceProfile.Update(insuranceUserProfile);

            //Create Audit thats user subscrption changed 
            var activityLog = new ActivityLog(insuranceUserProfile.Id, null, null, "Subscription was activated", ServiceNames.HealthInsured.ToString());
            _repoWrapper.ActivityLog.Create(activityLog);

            await _repoWrapper.Save();
        }

        /// <summary>
        /// Function to send email reminder three days before subscription cysle ends.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="userName"></param>
        /// <param name="context"></param>
        public void SendEmailReminder(string email,string userName, PerformContext context)
        {            
            _emailSender.SendHealthInsuredPaymentReminder(email,"Payment Reminder",userName);
        }        

        private async Task SendDetailsToInsuranceProvider(InsuranceUserProfile insuranceUserProfile)
        {
            if (insuranceUserProfile.InsuranceService.ToLower() == InsuranceProvider.Hygeia.ToString().ToLower())
            {
                var response = await _insuranceSerivce.EnrollUserToHygeiaOnOnboarding(insuranceUserProfile);
                if (response.Status)
                {
                    insuranceUserProfile.TransId = response.Message;
                }
                else
                {
                    insuranceUserProfile.TransId = "Pending";
                }
                _repoWrapper.InsuranceProfile.Update(insuranceUserProfile);
                await _repoWrapper.Save();
            }
            else
            {
                await _insuranceSerivce.EnrollUserToAxamansardOnOnboarding(insuranceUserProfile);
            }
        }
    } 
}

