
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

namespace Application.Services.HealthInsured
{
    public class TokenizationService
    {
        private readonly IMapper _mapper;
        private readonly PaystackService _paystackService;
        private readonly InsuranceService _insuranceSerivce;
        private readonly IEmailSender _emailSender;
        private readonly IRepositoryWrapper _repoWrapper;

        private SubscriptionDuration _subscriptionAccessor { get; }

        public TokenizationService(IMapper mapper, PaystackService paystackService,InsuranceService insuranceSerivce,
            IOptions<SubscriptionDuration> subscriptionAccessor, IEmailSender emailSender,IRepositoryWrapper repoWrapper)
        {
            _mapper = mapper;
            _paystackService = paystackService;
            _insuranceSerivce = insuranceSerivce;
            _emailSender = emailSender;
            _subscriptionAccessor = subscriptionAccessor.Value;
            _repoWrapper = repoWrapper;
        }

        /// <summary>
        /// This Method is when user tokenize their card. For a user adding card, Just a fee of 50 naira 
        /// is deducted to process their card and get an authorization code to use in future payments,charge is refunded after. For first time users tokenizing card 
        /// for the first time,a fee of 50 naira is used to test the card and get an authorization code,charge is refunded after.If their is a free trial,
        /// a scheduled job is used for future deductions.If theres is no free trial, subscription premium fee is paid and a scheduled job is used for
        /// future deductions.
        /// </summary>
        /// <param name="chargeCard"></param>
        /// <param name="id"></param>
        /// <param name="ipAddress"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> TokenizeCard(ChargeCardViewModel chargeCard, int id)
        {
            var profileCompletion = await _insuranceSerivce.GetProfileCompletion(id);
            if (profileCompletion.Data.CorporateUser is false)
            {
                return await ProcessIndividualCardTokenization(chargeCard, id);
            }
            else if(profileCompletion.Data.CorporateUser is true)
            {
                return await ProcessCorporateCardTokenization(chargeCard, id);
            }
            return new ResponseMessage { Message = "User does not have a profile,kindly create your profile", Status = false };
        }

        /// <summary>
        /// This is used to process card tokenization for inidvidual users.This method process the charge amount and process api Request that would be
        /// sent to paystack.
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

                // Check if user has card dat matches last four card digit
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

                // If the user is neither active nor  deactivated. Means user tokenizing for card first time
                if (insuranceProfile.SubscriptionStatus == null)
                {                   
                    card.amount = (insuranceProfile.Premium * 100).ToString();
                }
                // If the user is not tokenizing for the first time
                // we do a test charge of 50 naria to get authorization code to use in future transactions, we send amount in Kobo\
                else
                {
                    card.amount = (50 * 100).ToString();
                }
                
                // Create payment reference for the charge.
                var channel = insuranceProfile.InsuranceService == InsuranceProvider.Hygeia.ToString() ? PaymentReference_ChannelValue.healthinsured_hygeia.ToString() 
                    : PaymentReference_ChannelValue.healthinsured_axamansard.ToString();

                var paymentReference = new PaymentReference(channel,card.reference, insuranceProfile.Id, null, insuranceProfile.UserId
                , insuranceProfile.Premium, PaymentReference_StatusValue.Pending.ToString());
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
        /// This is used to process card tokenization for corporate org.This method process the charge amount and process api Request that would be
        /// sent to paystack. 
        /// </summary>
        /// <param name="chargeCard"></param>
        /// <param name="id"></param>
        /// <param name="ipAddress"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        private async Task<ResponseMessage> ProcessCorporateCardTokenization(ChargeCardViewModel chargeCard,  int id)
        {
            var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(id);
            if (companyProfile != null)
            {
                if (companyProfile.ProfileCompleted && companyProfile.EmailConfirmed)
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
                        if(totalAmount == 0)
                        {
                            return new ResponseMessage { Message = "Beneficiaries must be greater than 1" };
                        }
                        card.amount = (totalAmount * 100).ToString();
                    }
                    else
                    {
                        // When user is adding card
                        card.amount = (100 * 50).ToString();
                    }

                    // Create payment reference for the charge.
                    var channel = companyProfile.InsuranceService == InsuranceProvider.Hygeia.ToString() ? PaymentReference_ChannelValue.healthinsured_hygeia.ToString() : PaymentReference_ChannelValue.healthinsured_axamansard.ToString();
                    var paymentReference = new PaymentReference(channel,card.reference, null, companyProfile.Id, id, decimal.Parse(card.amount), PaymentReference_StatusValue.Pending.ToString());
                    _repoWrapper.PaymentReference.Create(paymentReference);
                    await _repoWrapper.Save();

                    // Paystack service to charge user card
                    var chargeCardResponse = await _paystackService.ChargeCard(card, id);

                    // function to process response from paystack
                    return await ProcessPaystackChargeCardResponse(chargeCardResponse, companyProfile, paymentReference,card.reference);
                }
                return new ResponseMessage { Message = "Kindly update your profile", Status = false };
            }
            return new ResponseMessage { Message = "User does not have a profile,kindly create your profile", Status = false };
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
        public async Task<ResponseMessage> ProcessPaystackChargeCardResponse(TokenizationResponse chargeCardResponse, InsuranceUserProfile insuranceUserProfile,
           InsuranceCompletionProfile checkprofileComplete,PaymentReference paymentReference, string cardReference)
        {      
            // if charge card was successfully
            if (chargeCardResponse.Status == true && chargeCardResponse.ResponseCode == 0)
            {
                //If card count is 0. it means there is no card available, so the card tokenised will
                //be the primary card so primary card status is set to 1 
                //else, card status is 0;
                var cardStatus = insuranceUserProfile.Cards.Count == 0 ? (int) DebitCard_StatusValue.primary : (int) DebitCard_StatusValue.secondary;

                var debitCard = new DebitCard(insuranceUserProfile.UserId, insuranceUserProfile.Id,null, cardStatus, chargeCardResponse.LastDigit, chargeCardResponse.Type
                    , cardReference, chargeCardResponse.AuthorizationCode);
                _repoWrapper.Card.Create(debitCard);

                checkprofileComplete.TokenizationCompleted = true;
                _repoWrapper.InsuranceProfile.Update(insuranceUserProfile);
                await _repoWrapper.Save();  

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
           PaymentReference paymentReference,string cardReference)
        {
            // if charge card was successfully
            if (chargeCardResponse.Status == true && chargeCardResponse.ResponseCode == 0)
            {
                //If card count is 0. it means there is no card available, so the card tokenised will
                //be the primary card so primary card status is set to 1 
                //else, card status is 0;

                var cardStatus = companyProfile.Cards.Count == 0 ? (int) DebitCard_StatusValue.primary : (int) DebitCard_StatusValue.secondary;

                var debitCard = new DebitCard(companyProfile.UserId, null, companyProfile.Id, cardStatus, chargeCardResponse.LastDigit, chargeCardResponse.Type
                    , cardReference, chargeCardResponse.AuthorizationCode);
                _repoWrapper.Card.Create(debitCard);

                if (!companyProfile.TokenizationCompleted)
                {
                    companyProfile.NextPaymentDate = DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration);

                    //method to create insurance profiles for the beneficiaires.
                   var result =  await _insuranceSerivce.CreateInsuranceProfileForCompanyBeneficiaries(companyProfile.Id, null);

                    if (result.Status)
                    {
                        //Background task to Enroll all users to hygeia.
                        BackgroundJob.Enqueue(() => _insuranceSerivce.OnboardUsersToHygeia(companyProfile.UserId, null, companyProfile.NextPaymentDate.Value));
                    }                   

                    //Background task to schedule debit at the end of next cycle
                    companyProfile.PendingJobId = await ProcessScheduledPayment(companyProfile);

                    companyProfile.PendingEmailJobId = BackgroundJob.Schedule(() => _insuranceSerivce.SendEmailReminder(companyProfile.CompanyEmail, companyProfile.CompanyName, null),
                          DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration).Subtract(new TimeSpan(3, 0, 0, 0)));

                    _repoWrapper.CompanyProfile.Update(companyProfile);
                    companyProfile.TokenizationCompleted = true;

                    var activityLog = new ActivityLog(null, companyProfile.Id, "Debit Card Added", ServiceNames.HealthInsured.ToString());
                    _repoWrapper.ActivityLog.Create(activityLog);
                }
                await _repoWrapper.Save();

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

        /// <summary>
        /// Overloaded method to process individual scheduled payment.Create scheduled enrollment and scheduled payment data that is set to the processing stage.
        /// </summary>
        /// <param name="insuranceProfile"></param>
        /// <returns></returns>
        public async Task<string> ProcessScheduledPayment(InsuranceUserProfile insuranceProfile)
        {
            var executionDate = insuranceProfile.EndActiveStatusDate;

            var jobId = BackgroundJob.Schedule(() => SchedulePaymentLogic(insuranceProfile.UserId,
                 insuranceProfile.Id, null), executionDate);

            if(insuranceProfile.InsuranceService.ToLower() == InsuranceProvider.Hygeia.ToString().ToLower())
            {
                var processingAxaEnrollment = new ScheduledEnrollment(insuranceProfile.Id, executionDate, jobId, ScheduledEnrollment_StatusValue.Processing.ToString(), null,
                    InsuranceProvider.Hygeia.ToString());
                _repoWrapper.ScheduledEnrollment.Create(processingAxaEnrollment);

                var processingScheduledPayment = new ScheduledPayment(insuranceProfile.UserId, insuranceProfile.Id, processingAxaEnrollment.Id,null,
                executionDate, jobId, ScheduledPayment_StatusValue.Processing.ToString(), null, null, InsuranceProvider.Hygeia.ToString());
                _repoWrapper.ScheduledPayment.Create(processingScheduledPayment);
            }
            else
            {
                var processingAxaEnrollment = new ScheduledEnrollment(insuranceProfile.Id, executionDate, jobId, ScheduledEnrollment_StatusValue.Processing.ToString(), null, 
                    InsuranceProvider.Axamansard.ToString());
                _repoWrapper.ScheduledEnrollment.Create(processingAxaEnrollment);

                var processingScheduledPayment = new ScheduledPayment(insuranceProfile.UserId, insuranceProfile.Id, processingAxaEnrollment.Id, null,
                executionDate, jobId, ScheduledPayment_StatusValue.Processing.ToString(), null, null, InsuranceProvider.Axamansard.ToString());
                _repoWrapper.ScheduledPayment.Create(processingScheduledPayment);
            } 
            await _repoWrapper.Save();

            return jobId;
        }

        /// <summary>
        /// Overloaded method to process corporate scheduled payment.Create scheduled enrollment and scheduled payment data that is set to the processing stage.
        /// </summary>
        /// <param name="insuranceProfile"></param>
        /// <returns></returns>
        public async Task<string> ProcessScheduledPayment(CompanyProfile companyProfile)
        {
            var executionDate = companyProfile.NextPaymentDate.Value;

            var jobId = BackgroundJob.Schedule(() => SchedulePaymentLogic(companyProfile.UserId, null), executionDate);

            var processingScheduledPayment = new ScheduledPayment(companyProfile.UserId, null, null, companyProfile.Id, executionDate, jobId, ScheduledPayment_StatusValue.Processing.ToString(), null
                , null, InsuranceProvider.Hygeia.ToString());
            _repoWrapper.ScheduledPayment.Create(processingScheduledPayment);
            await _repoWrapper.Save();
            return jobId;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task SchedulePaymentLogic(int userId, int axamansardUserId, PerformContext context)
        {
            var jobId = context.BackgroundJob.Id;
            var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByUserIdAsync(userId);
            var activeCard = insuranceProfile.Cards.FirstOrDefault(x => x.Status == (int) DebitCard_StatusValue.primary);

            //Get a charge authorization model to use in scheduled payment background process.
            var chageAuthorizationModel = new ChargeAuthorization()
            {
                email = insuranceProfile.Email,
                amount = (insuranceProfile.Premium * 100).ToString(),
                authorization_code = activeCard.Authorization_Code
            };

            var chargeAuthorization = await _paystackService.ChargeAuthorization(chageAuthorizationModel);
            var scheduledPaymentJob = await _repoWrapper.ScheduledPayment.GetScheduledPaymentByJobId(jobId);
            // Insufficient funds
            if (!chargeAuthorization.Status && chargeAuthorization.ResponseCode == 10)
            {
                var channel = insuranceProfile.InsuranceService == InsuranceProvider.Hygeia.ToString() ? PaymentReference_ChannelValue.healthinsured_hygeia.ToString() 
                    : PaymentReference_ChannelValue.healthinsured_axamansard.ToString();

                var paymentReference = new PaymentReference(channel,chargeAuthorization.Reference, insuranceProfile.Id,null, insuranceProfile.UserId
                    , insuranceProfile.Premium, PaymentReference_StatusValue.Failed.ToString());
                _repoWrapper.PaymentReference.Create(paymentReference);

                scheduledPaymentJob.Status = ScheduledPayment_StatusValue.Terminated.ToString(); scheduledPaymentJob.Message = chargeAuthorization.Message;
                scheduledPaymentJob.PaymentReference = chargeAuthorization.Reference;

                scheduledPaymentJob.ScheduledEnrollment.Status = ScheduledEnrollment_StatusValue.Terminated.ToString(); scheduledPaymentJob.ScheduledEnrollment.Message = "Terminated";
                scheduledPaymentJob.PaymentReference = chargeAuthorization.Reference;

                insuranceProfile.PendingJobId = null; insuranceProfile.SubscriptionStatus = false;
                insuranceProfile.ActiveStatus = false;
                _repoWrapper.InsuranceProfile.Update(insuranceProfile);
                await _repoWrapper.InsuranceProfile.Save();
                BackgroundJob.Delete(jobId);

                _insuranceSerivce.SendEmailOnFailedDebit(insuranceProfile.Email, insuranceProfile.Surname, insuranceProfile.Premium.ToString());
            }
            // Failed
            else
            {
                var channel = insuranceProfile.InsuranceService == InsuranceProvider.Hygeia.ToString() ? PaymentReference_ChannelValue.healthinsured_hygeia.ToString() : PaymentReference_ChannelValue.healthinsured_axamansard.ToString();

                var paymentReference = new PaymentReference(channel,chargeAuthorization.Reference, insuranceProfile.Id,null, insuranceProfile.UserId
                   , insuranceProfile.Premium, PaymentReference_StatusValue.Failed.ToString());
                _repoWrapper.PaymentReference.Create(paymentReference);

                scheduledPaymentJob.Status = ScheduledPayment_StatusValue.Failed.ToString(); scheduledPaymentJob.Message = chargeAuthorization.Message;
                scheduledPaymentJob.ScheduledEnrollment.Status = ScheduledEnrollment_StatusValue.Terminated.ToString(); scheduledPaymentJob.ScheduledEnrollment.Message = "Terminated";
                scheduledPaymentJob.PaymentReference = chargeAuthorization.Reference;
                _repoWrapper.ScheduledPayment.Update(scheduledPaymentJob);

                insuranceProfile.PendingJobId = null; insuranceProfile.SubscriptionStatus = false;
                insuranceProfile.ActiveStatus = false;
                _repoWrapper.InsuranceProfile.Update(insuranceProfile);
                await _repoWrapper.Save();
                BackgroundJob.Delete(jobId);

                _insuranceSerivce.SendEmailOnFailedDebit(insuranceProfile.Email, insuranceProfile.Surname, insuranceProfile.Premium.ToString());
            }
            await Task.CompletedTask;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task SchedulePaymentLogic(int userId, PerformContext context)
        {
            var jobId = context.BackgroundJob.Id;
            var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyInsuranceUserProfilesByCompanyId(userId);
            var activeCard = companyProfile.Cards.FirstOrDefault(x => x.Status == (int) DebitCard_StatusValue.primary);

            var premiumFee = companyProfile.InsuranceUserProfiles.Where(x => x.CompanySubscribedStatus == InsuranceProfile_CompanySubStatusValue.Active.ToString() ||
            x.CompanySubscribedStatus == InsuranceProfile_CompanySubStatusValue.Pending.ToString()).Select(x => x.Premium).Sum();            

            var scheduledPaymentJob = await _repoWrapper.ScheduledPayment.GetScheduledPaymentByJobId(jobId);
            if(scheduledPaymentJob is null)
            {
                scheduledPaymentJob = new ScheduledPayment();
                scheduledPaymentJob.JobId = jobId; scheduledPaymentJob.CompanyProfileId = companyProfile.Id; scheduledPaymentJob.UserId = companyProfile.UserId;
                scheduledPaymentJob.InsuranceService = companyProfile.InsuranceService;
                _repoWrapper.ScheduledPayment.Create(scheduledPaymentJob);
                await _repoWrapper.Save();
            }

            // set the next repayment date at first trial i.e when there is no failed payment attempt !!!!!
            if (companyProfile.FailedScheduledPaymentRetry is null)
            {
                companyProfile.NextPaymentDate = DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration);
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
                if (chargeAuthorization.Status)
                {
                    scheduledPaymentJob.Status = ScheduledPayment_StatusValue.Successful.ToString(); scheduledPaymentJob.Message = ScheduledPayment_StatusValue.Successful.ToString();
                    scheduledPaymentJob.PaymentReference = chargeAuthorization.Reference;

                    _repoWrapper.ScheduledPayment.Update(scheduledPaymentJob);

                    //Task to Enroll pending users to hygeia.
                    await _insuranceSerivce.OnboardUsersToHygeia(companyProfile.UserId, InsuranceProfile_CompanySubStatusValue.Pending.ToString(), companyProfile.NextPaymentDate.Value);

                    //Background task to schedule debit at the end of next cycle
                    companyProfile.PendingJobId = await ProcessScheduledPayment(companyProfile);

                    companyProfile.PendingEmailJobId = BackgroundJob.Schedule(() => _insuranceSerivce.SendEmailReminder(companyProfile.CompanyEmail, companyProfile.CompanyName, null),
                          DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration).Subtract(new TimeSpan(3, 0, 0, 0)));

                    companyProfile.FailedScheduledPaymentRetry = null;
                    companyProfile.TokenizationCompleted = true;

                    _repoWrapper.CompanyProfile.Update(companyProfile);

                    await _repoWrapper.Save();
                }
                // Insufficient funds
                else if(!chargeAuthorization.Status && chargeAuthorization.ResponseCode == 10)
                {
                    var channel = PaymentReference_ChannelValue.healthinsured_hygeia.ToString();

                    var paymentReference = new PaymentReference(channel, chargeAuthorization.Reference, null, companyProfile.Id, companyProfile.UserId
                        , premiumFee, PaymentReference_StatusValue.Failed.ToString());
                    _repoWrapper.PaymentReference.Create(paymentReference);

                    scheduledPaymentJob.Status = ScheduledPayment_StatusValue.Terminated.ToString(); scheduledPaymentJob.Message = chargeAuthorization.Message;
                    scheduledPaymentJob.PaymentReference = chargeAuthorization.Reference;
                    _repoWrapper.ScheduledPayment.Update(scheduledPaymentJob);


                    if (companyProfile.FailedScheduledPaymentRetry is null || companyProfile.FailedScheduledPaymentRetry < 2)
                    {
                        companyProfile.PendingEmailJobId = null; 

                        companyProfile.PendingJobId = BackgroundJob.Schedule(() => SchedulePaymentLogic(companyProfile.UserId, null), DateTime.Now.AddDays(_subscriptionAccessor.FailedDebitRetrialDuration));
                        companyProfile.FailedScheduledPaymentRetry += 1;
                        _repoWrapper.CompanyProfile.Update(companyProfile);
                        await _repoWrapper.Save();

                        _insuranceSerivce.SendCompanyEmailOnFailedDebit(companyProfile.CompanyEmail, companyProfile.CompanyName, premiumFee.ToString()
                            , companyProfile.NextPaymentDate.Value.AddDays(2).ToLongDateString());
                    }
                    else
                    {
                        companyProfile.PendingEmailJobId = null; 
                        companyProfile.PendingJobId = await ProcessScheduledPayment(companyProfile);
                        companyProfile.FailedScheduledPaymentRetry = null;
                        _repoWrapper.CompanyProfile.Update(companyProfile);
                        await _repoWrapper.Save();

                        await DeactivateAllCompanybeneficiaries(companyProfile.UserId);
                        //Send email
                    }
                }
                // Failed
                else
                {
                    var channel = PaymentReference_ChannelValue.healthinsured_hygeia.ToString();

                    var paymentReference = new PaymentReference(channel, chargeAuthorization.Reference, null, companyProfile.Id, companyProfile.UserId
                        , premiumFee, PaymentReference_StatusValue.Failed.ToString());
                    _repoWrapper.PaymentReference.Create(paymentReference);

                    scheduledPaymentJob.Status = ScheduledPayment_StatusValue.Failed.ToString(); scheduledPaymentJob.Message = chargeAuthorization.Message;
                    scheduledPaymentJob.PaymentReference = chargeAuthorization.Reference;

                    _repoWrapper.ScheduledPayment.Update(scheduledPaymentJob);


                    if (companyProfile.FailedScheduledPaymentRetry is null || companyProfile.FailedScheduledPaymentRetry < 2)
                    {
                        companyProfile.PendingEmailJobId = null; 

                        companyProfile.PendingJobId = BackgroundJob.Schedule(() => SchedulePaymentLogic(companyProfile.UserId, null), DateTime.Now.AddDays(_subscriptionAccessor.FailedDebitRetrialDuration));
                        companyProfile.FailedScheduledPaymentRetry += 1;
                        _repoWrapper.CompanyProfile.Update(companyProfile);
                        await _repoWrapper.Save();

                        _insuranceSerivce.SendCompanyEmailOnFailedDebit(companyProfile.CompanyEmail, companyProfile.CompanyName, premiumFee.ToString()
                            , companyProfile.NextPaymentDate.Value.AddDays(2).ToLongDateString());
                    }
                    else
                    {
                        companyProfile.PendingEmailJobId = null; 
                        companyProfile.PendingJobId = await ProcessScheduledPayment(companyProfile);
                        companyProfile.FailedScheduledPaymentRetry = null;
                        _repoWrapper.CompanyProfile.Update(companyProfile);
                        await _repoWrapper.Save();

                        await DeactivateAllCompanybeneficiaries(companyProfile.UserId);

                        //Send email  , Deactivate all users.
                    }
                }
            }
            else
            {
                scheduledPaymentJob.Status = ScheduledPayment_StatusValue.Successful.ToString(); scheduledPaymentJob.Message = "No users to make payment For";
                scheduledPaymentJob.PaymentReference = "";
                var newJobId2 = await ProcessScheduledPayment(companyProfile);
                companyProfile.PendingJobId = newJobId2;
                companyProfile.PendingEmailJobId = null;
                companyProfile.FailedScheduledPaymentRetry = null;
                _repoWrapper.CompanyProfile.Update(companyProfile);
                await _repoWrapper.Save();
            }
            await Task.CompletedTask;            
        }

        public async Task<ResponseMessage> SubmitOtp(SetOtpViewModel otpViewModel,int id)
        {
            var profileCompletion = await _insuranceSerivce.GetProfileCompletion(id);
            var paymentReference = await _repoWrapper.PaymentReference.GetByReference(otpViewModel.reference);
            if (profileCompletion.Data.CorporateUser is false)
            {
                var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByUserIdAsync(id);
                var checkprofileComplete = await _repoWrapper.InsuranceCompletionProfile.GetCompletionStateByUserId(id);

                var chargeCardResponse = await _paystackService.SendOtp(otpViewModel.otp, otpViewModel.reference, insuranceProfile.PhoneNumber
                       , insuranceProfile.DateOfBirth, otpViewModel.pin);
                return await ProcessPaystackChargeCardResponse(chargeCardResponse, insuranceProfile, checkprofileComplete,
                    paymentReference, otpViewModel.reference);
            }
            else if (profileCompletion.Data.CorporateUser is true)
            {
                var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(id);
                var chargeCardResponse = await _paystackService.SendOtp(otpViewModel.otp, otpViewModel.reference, companyProfile.PhoneNumber
                       ,DateTime.Now, otpViewModel.pin);
                return await ProcessPaystackChargeCardResponse(chargeCardResponse, companyProfile, paymentReference, otpViewModel.reference);
            }
            return new ResponseMessage { Message = "User does not have a profile,kindly create your profile", Status = false };
        }

        /// <summary>
        /// method to cancel individual users subcription
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> CancelSubscription(int userId,string reason)
        {
            var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByUserIdAsync(userId);
            if (insuranceProfile.SubscriptionStatus == false)
            {
                return new ResponseMessage { Message = "You have no subscription", Status = false };
            }

            var scheduledJobId = insuranceProfile.PendingJobId;

            // Check if user has a pending scheduled debit
            var scheduledPayment = await _repoWrapper.ScheduledPayment.GetScheduledPaymentByJobId(scheduledJobId);   
            if(scheduledPayment != null)
            {
                BackgroundJob.Delete(scheduledJobId);
                if(insuranceProfile.PendingEmailJobId != null)
                {
                    BackgroundJob.Delete(insuranceProfile.PendingEmailJobId);
                }
                scheduledPayment.Status = ScheduledPayment_StatusValue.Cancelled.ToString(); scheduledPayment.Message = "Cancelled";
                scheduledPayment.ScheduledEnrollment.Status = ScheduledEnrollment_StatusValue.Cancelled.ToString(); scheduledPayment.ScheduledEnrollment.Message = "Cancelled";
                _repoWrapper.ScheduledPayment.Update(scheduledPayment);
            }
            else
            {
                //Check if a user has a pending debit on scheduled subscription reactivation
                var scheduledReactivatedPayment = await _repoWrapper.PaymentOnReactivation.GetScheduledPaymentByJobId(scheduledJobId, insuranceProfile.UserId);
                if(scheduledReactivatedPayment != null)
                {
                    BackgroundJob.Delete(scheduledJobId);
                    if (insuranceProfile.PendingEmailJobId != null)
                    {
                        BackgroundJob.Delete(insuranceProfile.PendingEmailJobId);
                    }
                    scheduledReactivatedPayment.Status = "Cancelled"; scheduledReactivatedPayment.Message = "Cancelled";
                    scheduledReactivatedPayment.EnrollmentOnReactivation.Status = "Cancelled";
                    scheduledReactivatedPayment.EnrollmentOnReactivation.Message = "Cancelled";
                    _repoWrapper.PaymentOnReactivation.Update(scheduledReactivatedPayment);
                }
            }

            var daysToCancelUserActivityStatus = insuranceProfile.EndActiveStatusDate;
            // check if date active cycle will end correspond with present date. if so set active cycle to false.
            //if (daysToCancelUserActivityStatus.Date == DateTime.Now.Date)
            if (daysToCancelUserActivityStatus == DateTime.Now)
            {
                insuranceProfile.ActiveStatus = false;
                insuranceProfile.PendingJobId = null;
                insuranceProfile.PendingEmailJobId = null;
                if(insuranceProfile.InsuranceService == InsuranceProvider.Hygeia.ToString())
                {
                    await _insuranceSerivce.HygeiaDeactivateUser(insuranceProfile.TransId);
                }
            }
            else
            {
                // Schedule task to render user status inactive when cycle ends
                var jobId = BackgroundJob.Schedule(() => ProcessUserActiveStatusCancellation(insuranceProfile.Id), daysToCancelUserActivityStatus);
                insuranceProfile.PendingJobId = jobId;
                insuranceProfile.PendingEmailJobId = null;
            }

            insuranceProfile.SubscriptionStatus = false;
            _repoWrapper.InsuranceProfile.Update(insuranceProfile);

            var activityLog = new ActivityLog(insuranceProfile.Id, null, "Subscription Was Cancelled Successfully", ServiceNames.HealthInsured.ToString());
            _repoWrapper.ActivityLog.Create(activityLog);

            await _repoWrapper.Save();
            var profileDTO = _mapper.Map<IndividualProfileDTO>(insuranceProfile);
            return new ResponseMessage { Data = profileDTO, Message = "Subscription was canceled successfully.However you remain active till " +
                "your insurance cycle ends.", Status = true };
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
                        BackgroundJob.Schedule(() => ProcessUserActiveStatusCancellation(insuranceUserProfile.Id),insuranceUserProfile.EndActiveStatusDate);
                    }                   
                    _repoWrapper.InsuranceProfile.Update(insuranceUserProfile);
                }
            }
            await _repoWrapper.Save();
            return new ResponseMessage { Data = "Beneficiariary was deactivated successfully,beneficiary will remain active and would be moved to the inactive list at the end of current cycle.", Status = true,
                Message= "Beneficiary was deactivated successfully,beneficiary will remain active and would be moved to the inactive list at the end of current cycle."};
        }

        /// <summary>
        /// Deactivate all company insurance users
        /// </summary>
        /// <param name="companyUserId"></param>
        /// <returns></returns>
        public async Task DeactivateAllCompanybeneficiaries(int companyUserId)
        {
            var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(companyUserId);
            foreach(var item in companyProfile.InsuranceUserProfiles)
            {
                await _insuranceSerivce.HygeiaDeactivateUser(item.TransId);
                item.ActiveStatus = false;
                item.CompanySubscribedStatus = InsuranceProfile_CompanySubStatusValue.Inactive.ToString();
                item.SubscriptionStatus = false;
                _repoWrapper.InsuranceProfile.Update(item);
                await _repoWrapper.Save();
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task ProcessUserActiveStatusCancellation(int userId)
        {
            var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByIdAsync(userId);
            if (insuranceProfile.InsuranceService.ToLower() == InsuranceProvider.Hygeia.ToString().ToLower())
            {
                await _insuranceSerivce.HygeiaDeactivateUser(insuranceProfile.TransId);
            }
            insuranceProfile.ActiveStatus = false;
            _repoWrapper.InsuranceProfile.Update(insuranceProfile);
            await _repoWrapper.Save();
            await Task.CompletedTask;
        }

        /// <summary>
        /// Service to change primary card
        /// </summary>
        /// <param name="newCardId"></param>
        /// <param name="userId"></param>
        /// <param name="ipAddress"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> ChangePrimaryCard(int newCardId,int userId)
        {
            var presentPrimaryCard = await _repoWrapper.Card.GetPrimaryCard(userId);
            if (presentPrimaryCard == null)
            {
                return new ResponseMessage { Message = "You dont have a debit card, Kindly add one" };
            }

            var newPrimaryCard = await _repoWrapper.Card.GetCardByIdAsync(newCardId, userId);
            if (newPrimaryCard == null)
            {
                return new ResponseMessage { Message = "Card was not found", ResponseCode=12 };
            }
            if (newPrimaryCard.Status == (int) DebitCard_StatusValue.primary)
            {
                return new ResponseMessage { Message = "This card is presently the primary card" };
            }
            presentPrimaryCard.Status = (int) DebitCard_StatusValue.secondary;
            _repoWrapper.Card.Update(presentPrimaryCard);
            newPrimaryCard.Status = (int) DebitCard_StatusValue.primary;
            _repoWrapper.Card.Update(newPrimaryCard);

            var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByUserIdAsync(userId);
            if(insuranceProfile != null)
            {
                var activityLog = new ActivityLog(insuranceProfile.Id, null, "Change Primary Card", ServiceNames.HealthInsured.ToString());
                _repoWrapper.ActivityLog.Create(activityLog);
            }
            else
            {
                var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(userId);
                var activityLog = new ActivityLog(null, companyProfile.Id, "Change Primary Card", ServiceNames.HealthInsured.ToString());
                _repoWrapper.ActivityLog.Create(activityLog);
            }          

            await _repoWrapper.Save();
            return new ResponseMessage { Message = "Primary card was changed successfully", Status=true };
        }                 

        /// <summary>
        /// Service to delete debit card
        /// </summary>
        /// <param name="cardId"></param>
        /// <param name="userId"></param>
        /// <param name="ipAddress"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> DeleteCard(int cardId,int userId)
        {
            var card = await _repoWrapper.Card.GetCardByIdAsync(cardId, userId);
            if (card == null) return new ResponseMessage { Message = "Card  was not found", ResponseCode = 12 };

            var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByUserIdAsync(userId);
            if(insuranceProfile != null)
            {
                if (card.Status == (int)DebitCard_StatusValue.primary && insuranceProfile.SubscriptionStatus == true) return new ResponseMessage
                {
                    Message = "Kindly set a new card as" +
                   "primary card to delete present primary card"
                };
                _repoWrapper.Card.Delete(card);

                var activityLog = new ActivityLog(insuranceProfile.Id, null, "Debit Card Was Removed", ServiceNames.HealthInsured.ToString());
                _repoWrapper.ActivityLog.Create(activityLog);
            }
            else
            {
                var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(userId);
                if (card.Status == (int)DebitCard_StatusValue.primary) return new ResponseMessage
                {
                    Message = "Kindly set a new card as" +
                  "primary card to delete present primary card"
                };
                _repoWrapper.Card.Delete(card);

                var activityLog = new ActivityLog(null, companyProfile.Id, "Debit Card Was Removed", ServiceNames.HealthInsured.ToString());
                _repoWrapper.ActivityLog.Create(activityLog);

            }                     
            await _repoWrapper.Save();

            return new ResponseMessage { Message = "Card was deleted successfully", Status = true };
        }

        /// <summary>
        /// Fetch debit card
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> GetCards(int userId)
        {
            var profileCompletion = await _insuranceSerivce.GetProfileCompletion(userId);
            if (profileCompletion.Data.CorporateUser is false)
            {
                var individualInsuranceUser = await _repoWrapper.InsuranceProfile.GetByUserIdAsync(userId);
                if (individualInsuranceUser != null)
                {
                    if (individualInsuranceUser.Cards.Count > 0)
                    {
                        var cardDTO = _mapper.Map<List<DebitCard>, List<CardDTO>>(individualInsuranceUser.Cards);
                        return new ResponseMessage { Data = cardDTO, Status = true, Message = "Card was fetchd successfully" };
                    }
                    var emptyCardDTO = new List<CardDTO>();
                    return new ResponseMessage { Data = emptyCardDTO, Message = "User has no card, Kindly add a card", Status = true };
                }
            }
            else if (profileCompletion.Data.CorporateUser is true)
            {
                var coporateInsuranceUser = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(userId);
                if (coporateInsuranceUser != null)
                {
                    if (coporateInsuranceUser.Cards.Count > 0)
                    {
                        var cardDTO = _mapper.Map<List<DebitCard>, List<CardDTO>>(coporateInsuranceUser.Cards);
                        return new ResponseMessage { Data = cardDTO, Status = true, Message = "Card was fetchd successfully" };
                    }
                    var emptyCardDTO = new List<CardDTO>();
                    return new ResponseMessage { Data = emptyCardDTO, Message = "User has no card, Kindly add a card", Status = true };
                }
            }
            return new ResponseMessage { Status = false, Message = "User was not found"};
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
            var primaryCard = insuranceProfile.Cards.FirstOrDefault(x => x.Status == (int) DebitCard_StatusValue.primary);
            if (primaryCard == null)
            {
                return new ResponseMessage { Message = "Kindly add a primary card, then start the reactivation process" };
            }

            // Check is user is in an active cycle
            if (insuranceProfile.ActiveStatus == false)
            {
                return await ProcessReactivationFlow(insuranceProfile, primaryCard.Authorization_Code,null);
            }
            else
            {
                return await ProcessReactivationFlow(insuranceProfile, primaryCard.Authorization_Code
                    , insuranceProfile.EndActiveStatusDate);
            }
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
        private async Task<ResponseMessage> ProcessReactivationFlow(InsuranceUserProfile insuranceProfile, string authorization_Code,DateTime? reactivationTime)
        {            
            if (reactivationTime is null)
            {   
                // Process immeediate reactivation
                return await ProcessImmediateReactivationPayment(insuranceProfile, authorization_Code);
            }
            else
            {
                // If user is still in active cycle and want to reactivate, that means user must have a background job scheduled to render user INACTIVE
                // at the end of current cycle.
                // To resolve this, we delete background task scheduled to render user inactive at the end of current cycle which is ProcessUserActiveStatusCancellation            
                BackgroundJob.Delete(insuranceProfile.PendingJobId);

                // Task to process scheduled payment at end of current active cycle
                var jobId = await ProcessScheduledPayment(insuranceProfile);

                // Schedule debit email reminder for user 
                insuranceProfile.PendingEmailJobId = BackgroundJob.Schedule(() => SendEmailReminder(insuranceProfile.Email, insuranceProfile.Surname, null), insuranceProfile.EndActiveStatusDate.Subtract(new TimeSpan(3, 0, 0, 0)));

                insuranceProfile.SubscriptionStatus = true;
                insuranceProfile.ActiveStatus = true;
                insuranceProfile.PendingJobId = jobId;
                _repoWrapper.InsuranceProfile.Update(insuranceProfile);
                await _repoWrapper.Save();
                return new ResponseMessage {Status = true,Message= "Reactivation was successful.You will be debited at the end of your " +
                    "active cycle" };
            }            
        }

        /// <summary>
        /// Func to process immediate user reactivation
        /// </summary>
        /// <param name="userAxamansardProfile"></param>
        /// <param name="authorization_Code"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> ProcessImmediateReactivationPayment(InsuranceUserProfile insuranceProfile, string authorization_Code)
        {
            var chageAuthorizationModel = new ChargeAuthorization()
            {
                email = insuranceProfile.Email,
                amount = (insuranceProfile.Premium * 100).ToString(),
                authorization_code = authorization_Code
            };

            // Call Paystack service. Debit user using card authorization code
            var chargeAuthorization = await _paystackService.ChargeAuthorization(chageAuthorizationModel);
            if (chargeAuthorization.Status)
            {
                Thread.Sleep(10000);
                return new ResponseMessage { Message = "Reactivation was successful." , Status = true, ResponseCode = chargeAuthorization.ResponseCode };
            }
            // Setfailed payment reference for transacation;
            var channel = insuranceProfile.InsuranceService == InsuranceProvider.Hygeia.ToString() ? PaymentReference_ChannelValue.healthinsured_hygeia.ToString() 
                : PaymentReference_ChannelValue.healthinsured_axamansard.ToString();

            var paymentReference = new PaymentReference(channel,chargeAuthorization.Reference, insuranceProfile.Id,null, insuranceProfile.UserId
                   , insuranceProfile.Premium, PaymentReference_StatusValue.Failed.ToString());
            _repoWrapper.PaymentReference.Create(paymentReference);
            await _repoWrapper.Save();

            return new ResponseMessage { Message = chargeAuthorization.Message, Status =false, ResponseCode = chargeAuthorization.ResponseCode };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="event"></param>
        /// <param name="email"></param>
        /// <param name="reference"></param>
        /// <param name="authorization_code"></param>
        /// <param name="last4"></param>
        /// <param name="card_type"></param>
        /// <returns></returns>
        public async Task ProcessPaystackWebHook(string @event, string email, string reference, string authorization_code, string last4, string card_type, string amount)
        {
            if (@event == "charge.success")
            {
                var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByEmail(email);
                if (insuranceProfile != null)
                {
                    var paymentReference = await _repoWrapper.PaymentReference.GetByReference(reference);
                    if (paymentReference != null)
                    {
                        paymentReference.Status = PaymentReference_StatusValue.Successful.ToString();
                        _repoWrapper.PaymentReference.Update(paymentReference);
                    }
                    else
                    {
                        // Create payment reference for the charge.
                        var channel = insuranceProfile.InsuranceService == InsuranceProvider.Hygeia.ToString() ? PaymentReference_ChannelValue.healthinsured_hygeia.ToString()
                            : PaymentReference_ChannelValue.healthinsured_axamansard.ToString();

                        var paymentReference2 = new PaymentReference(channel, reference, insuranceProfile.Id, null, insuranceProfile.UserId
                        , decimal.Parse(amount), PaymentReference_StatusValue.Successful.ToString());
                        _repoWrapper.PaymentReference.Create(paymentReference2);
                    }
                    await _repoWrapper.Save();
                    await ValidateWebHookSuccesfulInsurancePayment(insuranceProfile, null, reference, authorization_code, last4, card_type, amount);
                }
                else
                {
                    var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyProfileByEmail(email);
                    if (companyProfile != null)
                    {
                        var paymentReference = await _repoWrapper.PaymentReference.GetByReference(reference);
                        if (paymentReference != null)
                        {
                            paymentReference.Status = PaymentReference_StatusValue.Successful.ToString();
                            _repoWrapper.PaymentReference.Update(paymentReference);
                        }
                        else
                        {
                            // Create payment reference for the charge.
                            var channel = companyProfile.InsuranceService == InsuranceProvider.Hygeia.ToString() ? PaymentReference_ChannelValue.healthinsured_hygeia.ToString()
                                : PaymentReference_ChannelValue.healthinsured_axamansard.ToString();

                            var paymentReference2 = new PaymentReference(channel, reference, null, companyProfile.Id, companyProfile.UserId
                            , decimal.Parse(amount), PaymentReference_StatusValue.Successful.ToString());
                            _repoWrapper.PaymentReference.Create(paymentReference2);
                        }
                        await _repoWrapper.Save();
                        await ValidateWebHookSuccesfulInsurancePayment(null, companyProfile, reference, authorization_code, last4, card_type, amount);
                    }
                }
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Method to make sure Users get value for their insurance payment
        /// </summary>
        /// <returns></returns>
        private async Task ValidateWebHookSuccesfulInsurancePayment(InsuranceUserProfile insuranceUserProfile, CompanyProfile companyProfile, string reference, string authorization_code, string last4, string card_type,string amount)
        {
            if (!(insuranceUserProfile is null))
            {
                // check if user card exist, if not add it
                var card = await _repoWrapper.Card.CheckIfCardWasPreviouslyTokenized(insuranceUserProfile.UserId, last4);
                if (card is null)
                {
                    var cardStatus = insuranceUserProfile.Cards.Count == 0 ? (int)DebitCard_StatusValue.primary : (int)DebitCard_StatusValue.secondary;

                    var debitCard = new DebitCard(insuranceUserProfile.UserId, insuranceUserProfile.Id, null, cardStatus, last4, card_type
                        , reference, authorization_code);
                    _repoWrapper.Card.Create(debitCard);
                }
                await ProcessWebHook_SuccessfulInsuranceIndividualPayment(insuranceUserProfile, reference,amount);
            }
            //if (!(companyProfile is null))
            //{
            //    // check if user card exist, if not add it
            //    var card = await _repoWrapper.Card.CheckIfCardWasPreviouslyTokenized(companyProfile.UserId, last4);
            //    if (card is null)
            //    {
            //        var cardStatus = companyProfile.Cards.Count == 0 ? (int)DebitCard_StatusValue.primary : (int)DebitCard_StatusValue.secondary;

            //        var debitCard = new DebitCard(companyProfile.UserId, null, companyProfile.Id, cardStatus, last4, card_type
            //            , reference, authorization_code);
            //        _repoWrapper.Card.Create(debitCard);
            //    }
            //    await ProcessWebHook_SuccessfulCorporatePayment(companyProfile, reference, amount);
            //}
        }

        private async Task ProcessWebHook_SuccessfulInsuranceIndividualPayment(InsuranceUserProfile insuranceUserProfile, string reference, string amount)
        {
            if(decimal.Parse(amount) > 100)
            {
                // If user is making payment for the first time, or making an immediate reactivation
                if(insuranceUserProfile.SubscriptionStatus is null || (insuranceUserProfile.SubscriptionStatus is false && insuranceUserProfile.ActiveStatus is false))
                {
                    //Send user details to insurance provider when payment is successfully
                    if (insuranceUserProfile.InsuranceService.ToLower() == InsuranceProvider.Hygeia.ToString().ToLower())
                    {
                        var response = await _insuranceSerivce.EnrollUserToHygeiaOnOnboarding(insuranceUserProfile);
                        if (response.Status)
                        {
                            insuranceUserProfile.TransId = response.Message;
                        }
                        else
                        {
                            insuranceUserProfile.TransId = "NA";
                        }
                    }
                    else
                    {
                        await _insuranceSerivce.EnrollUserToAxamansardOnOnboarding(insuranceUserProfile);
                    }
                }

                // If payment is scheduled
                if(insuranceUserProfile.SubscriptionStatus is true && insuranceUserProfile.ActiveStatus is true)
                {
                    await ProcessWebHook_SuccessfulInsuranceIndividualPayment_ScheduledPayment(insuranceUserProfile, reference);
                }

                // If user is making payment for the first time,
                if (insuranceUserProfile.SubscriptionStatus == null)
                {
                    await ProcessWebHook_SuccessfulInsuranceIndividualPayment_FirstTimePayment(insuranceUserProfile);
                }

                // if user is  making an immediate reactivation or if it is a scheduled debit
                if (insuranceUserProfile.SubscriptionStatus is false && insuranceUserProfile.ActiveStatus is false)
                {
                    await ProcessWebHook_SuccessfulInsuranceIndividualPayment_ImmediateReactivationPayment(insuranceUserProfile);
                }
            }            
            // Enqueue method to process refund 0f 50 naira test charge
            else
            {
                BackgroundJob.Enqueue(() => _paystackService.RefundTestCardFunds(reference, (50 * 100).ToString()));
            }
        }

        private async Task ProcessWebHook_SuccessfulInsuranceIndividualPayment_FirstTimePayment(InsuranceUserProfile insuranceUserProfile)
        {
            insuranceUserProfile.EndActiveStatusDate = _subscriptionAccessor.FreeTrial is true ? DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration * 2)
                        : DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration);

            // Schedule debit email reminder for user 
            insuranceUserProfile.PendingEmailJobId = _subscriptionAccessor.FreeTrial is true ? BackgroundJob.Schedule(() => _insuranceSerivce.SendEmailReminder(insuranceUserProfile.Email, insuranceUserProfile.Surname, null),
                   DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration * 2).Subtract(new TimeSpan(3, 0, 0, 0)))
               : BackgroundJob.Schedule(() => _insuranceSerivce.SendEmailReminder(insuranceUserProfile.Email, insuranceUserProfile.Surname, null),
                   DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration).Subtract(new TimeSpan(3, 0, 0, 0)));

            //Schedule job to debit user every 28 days
            insuranceUserProfile.PendingJobId = await ProcessScheduledPayment(insuranceUserProfile);

            var checkprofileComplete = await _repoWrapper.InsuranceCompletionProfile.GetCompletionStateByUserId(insuranceUserProfile.UserId);
            checkprofileComplete.TokenizationCompleted = true;
            _repoWrapper.InsuranceCompletionProfile.Update(checkprofileComplete);

            _insuranceSerivce.SendSuccesfulSubscriptionMail(insuranceUserProfile.Email, insuranceUserProfile.Surname, insuranceUserProfile.TransId, insuranceUserProfile.CareProviderName);

            var activityLog2 = new ActivityLog(insuranceUserProfile.Id, null, "Debit Card Added", ServiceNames.HealthInsured.ToString());
            _repoWrapper.ActivityLog.Create(activityLog2);

            insuranceUserProfile.SubscriptionStatus = true;
            insuranceUserProfile.ActiveStatus = true;
            insuranceUserProfile.StartActiveStatusDate = DateTime.Now;

            _repoWrapper.InsuranceProfile.Update(insuranceUserProfile);

            //Create Audit thats user subscrption changed 
            var activityLog = new ActivityLog(insuranceUserProfile.Id, null, "Subscription was activated", ServiceNames.HealthInsured.ToString());
            _repoWrapper.ActivityLog.Create(activityLog);
            await _repoWrapper.Save();
        }

        private async Task ProcessWebHook_SuccessfulInsuranceIndividualPayment_ImmediateReactivationPayment(InsuranceUserProfile insuranceUserProfile)
        {
            insuranceUserProfile.EndActiveStatusDate = DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration);

            //Schedule job to debit user every 28 days
            insuranceUserProfile.PendingJobId = await ProcessScheduledPayment(insuranceUserProfile);

            // Schedule debit email reminder for user 
            insuranceUserProfile.PendingEmailJobId = BackgroundJob.Schedule(() => SendEmailReminder(insuranceUserProfile.Email, insuranceUserProfile.Surname, null), insuranceUserProfile.EndActiveStatusDate.Subtract(new TimeSpan(3, 0, 0, 0)));

            insuranceUserProfile.SubscriptionStatus = true;
            insuranceUserProfile.ActiveStatus = true;
            insuranceUserProfile.StartActiveStatusDate = DateTime.Now;
            _repoWrapper.InsuranceProfile.Update(insuranceUserProfile);

            //Create Audit thats user subscrption changed 
            var activityLog = new ActivityLog(insuranceUserProfile.Id, null, "Subscription was activated", ServiceNames.HealthInsured.ToString());
            _repoWrapper.ActivityLog.Create(activityLog);

            await _repoWrapper.Save();
        }

        private async Task ProcessWebHook_SuccessfulInsuranceIndividualPayment_ScheduledPayment(InsuranceUserProfile insuranceUserProfile, string reference)
        {
            var scheduledPaymentJob = await _repoWrapper.ScheduledPayment.GetScheduledPaymentByJobId(insuranceUserProfile.PendingJobId);
            scheduledPaymentJob.Status = ScheduledPayment_StatusValue.Successful.ToString(); scheduledPaymentJob.Message = ScheduledPayment_StatusValue.Successful.ToString();
            scheduledPaymentJob.PaymentReference = reference;

            _repoWrapper.ScheduledPayment.Update(scheduledPaymentJob);

            if (insuranceUserProfile.InsuranceService.ToLower() != InsuranceProvider.Hygeia.ToString().ToLower() || insuranceUserProfile.InsuranceService == null)
            {
                await _insuranceSerivce.EnrollUserToAxamansardOnOnboarding(insuranceUserProfile);
            }

            insuranceUserProfile.EndActiveStatusDate = DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration);

            //Schedule job to debit user every 28 days
            insuranceUserProfile.PendingJobId = await ProcessScheduledPayment(insuranceUserProfile);

            // Schedule debit email reminder for user 
            insuranceUserProfile.PendingEmailJobId = BackgroundJob.Schedule(() => SendEmailReminder(insuranceUserProfile.Email, insuranceUserProfile.Surname, null), insuranceUserProfile.EndActiveStatusDate.Subtract(new TimeSpan(3, 0, 0, 0)));

            insuranceUserProfile.SubscriptionStatus = true;
            insuranceUserProfile.ActiveStatus = true;
            insuranceUserProfile.StartActiveStatusDate = DateTime.Now;

            _repoWrapper.InsuranceProfile.Update(insuranceUserProfile);
            await _repoWrapper.Save();
        }

        //private async Task ProcessWebHook_SuccessfulCorporatePayment(CompanyProfile companyProfile, string reference,string amount)
        //{
        //    if(decimal.Parse(amount) > 100)
        //    {
        //        if (companyProfile.TokenizationCompleted)
        //        {
        //            var scheduledPaymentJob = await _repoWrapper.ScheduledPayment.GetScheduledPaymentByJobId(companyProfile.PendingJobId);
        //            scheduledPaymentJob.Status = ScheduledPayment_StatusValue.Successful.ToString(); scheduledPaymentJob.Message = ScheduledPayment_StatusValue.Successful.ToString();
        //            scheduledPaymentJob.PaymentReference = reference;

        //            _repoWrapper.ScheduledPayment.Update(scheduledPaymentJob);

        //            //Background task to Enroll pending users to hygeia.
        //            BackgroundJob.Enqueue(() => _insuranceSerivce.OnboardUsersToHygeia(companyProfile.UserId, InsuranceProfile_CompanySubStatusValue.Pending.ToString()));

        //            companyProfile.NextPaymentDate = DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration);

        //            //Background task to schedule debit at the end of next cycle
        //            companyProfile.PendingJobId = await ProcessScheduledPayment(companyProfile);

        //            companyProfile.PendingEmailJobId = BackgroundJob.Schedule(() => _insuranceSerivce.SendEmailReminder(companyProfile.CompanyEmail, companyProfile.CompanyName, null),
        //                  DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration).Subtract(new TimeSpan(3, 0, 0, 0)));

        //            companyProfile.TokenizationCompleted = true;

        //            _repoWrapper.CompanyProfile.Update(companyProfile);

        //            await _repoWrapper.Save();
        //        }
        //    }            
        //    // Enqueue method to process refund 0f 50 naira test charge
        //    else
        //    {
        //        BackgroundJob.Enqueue(() => _paystackService.RefundTestCardFunds(reference, (50 * 100).ToString()));
        //    }
        //    await _repoWrapper.Save();
        //}

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

        //Deprecated to be removed in future versions
        /// <summary>
        /// Background Task Func to process scheduled Reactivation.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="authorization_Code"></param>
        /// <returns></returns>
        [AutomaticRetry(Attempts = 0)]
        public async Task ProcessScheduledReactivationPayment(int userId, string authorization_Code)
        {
            var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByUserIdAsync(userId);

            var chageAuthorizationModel = new ChargeAuthorization()
            {
                email = insuranceProfile.Email,
                amount = (insuranceProfile.Premium * 100).ToString(),
                authorization_code = authorization_Code
            };

            // Call Paystack service. Debit user using card authorization code
            var chargeAuthorization = await _paystackService.ChargeAuthorization(chageAuthorizationModel);

            // Get  payment on reactivation data. 
            var paymentOnReactivation = await _repoWrapper.PaymentOnReactivation.GetScheduledPaymentByJobId(insuranceProfile.PendingJobId, insuranceProfile.UserId);
            if (chargeAuthorization.Status)
            {
                paymentOnReactivation.Status = PaymentOnReactivation_StatusValue.Successful.ToString(); paymentOnReactivation.Message = chargeAuthorization.Message;
                paymentOnReactivation.PaymentReference = chargeAuthorization.Reference;

                if (insuranceProfile.InsuranceService.ToLower() == InsuranceProvider.Hygeia.ToString())
                {
                    // Send user details to hygeia
                    var registrationModel = _mapper.Map<RegistrationModel>(insuranceProfile);
                    var registration = await _insuranceSerivce.HygeiaRegisterUser(registrationModel);
                    if (registration.Status)
                    {
                        paymentOnReactivation.EnrollmentOnReactivation.Status = EnrollmentOnReactivation_StatusValue.Successful.ToString(); paymentOnReactivation.EnrollmentOnReactivation.Message = registration.Message;
                    }
                    else
                    {
                        paymentOnReactivation.EnrollmentOnReactivation.Status = EnrollmentOnReactivation_StatusValue.Failed.ToString(); paymentOnReactivation.EnrollmentOnReactivation.Message = registration.Message;
                    }
                }
                else
                {
                    var enrollmentModel = _mapper.Map<EnrollmentModel>(insuranceProfile);

                    // Send user details to axamansard
                    var enrollment = await _insuranceSerivce.AxamansardRegisterUser(enrollmentModel);

                    if (enrollment.Status)
                    {
                        paymentOnReactivation.EnrollmentOnReactivation.Status = EnrollmentOnReactivation_StatusValue.Successful.ToString(); paymentOnReactivation.EnrollmentOnReactivation.Message = enrollment.Message;
                    }
                    else
                    {
                        paymentOnReactivation.EnrollmentOnReactivation.Status = EnrollmentOnReactivation_StatusValue.Failed.ToString(); paymentOnReactivation.EnrollmentOnReactivation.Message = enrollment.Message;
                    }
                }
                _repoWrapper.PaymentOnReactivation.Update(paymentOnReactivation);

                // Set the EndActiveStatusDate for the insurance profile before calling the background process ProcessScheduledPayment !!!!
                insuranceProfile.StartActiveStatusDate = DateTime.Now;
                insuranceProfile.EndActiveStatusDate = DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration);

                //Schedule job to debit user every 28 days
                var getScheduledPaymentJobId = await ProcessScheduledPayment(insuranceProfile);

                // Schedule debit email reminder for user 
                var emailReminderJobId = BackgroundJob.Schedule(() => SendEmailReminder(insuranceProfile.Email, insuranceProfile.Surname, null),
                    DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration).Subtract(new TimeSpan(3, 0, 0, 0)));

                insuranceProfile.PendingEmailJobId = emailReminderJobId;
                insuranceProfile.PendingJobId = getScheduledPaymentJobId;
                insuranceProfile.SubscriptionStatus = true; insuranceProfile.ActiveStatus = true;
                _repoWrapper.InsuranceProfile.Update(insuranceProfile);
            }
            // if user has insufficient funds during charge process
            else if (!chargeAuthorization.Status && chargeAuthorization.ResponseCode == 10)
            {
                // Set payment reference for failed transacation;
                var channel = insuranceProfile.InsuranceService == InsuranceProvider.Hygeia.ToString() ? PaymentReference_ChannelValue.healthinsured_hygeia.ToString() : PaymentReference_ChannelValue.healthinsured_axamansard.ToString();

                var paymentReference = new PaymentReference(channel, chargeAuthorization.Reference, insuranceProfile.Id, null, insuranceProfile.UserId
                   , insuranceProfile.Premium, PaymentReference_StatusValue.Failed.ToString());
                _repoWrapper.PaymentReference.Create(paymentReference);
                await _repoWrapper.Save();

                paymentOnReactivation.Status = PaymentOnReactivation_StatusValue.Terminated.ToString(); paymentOnReactivation.Message = chargeAuthorization.Message;
                paymentOnReactivation.PaymentReference = chargeAuthorization.Reference;
                paymentOnReactivation.EnrollmentOnReactivation.Status = EnrollmentOnReactivation_StatusValue.Terminated.ToString(); paymentOnReactivation.EnrollmentOnReactivation.Message = "Terminated";
                _repoWrapper.PaymentOnReactivation.Update(paymentOnReactivation);

                insuranceProfile.PendingJobId = null; insuranceProfile.SubscriptionStatus = false;
                insuranceProfile.ActiveStatus = false;
                _repoWrapper.InsuranceProfile.Update(insuranceProfile);

                _insuranceSerivce.SendEmailOnFailedDebit(insuranceProfile.Email, insuranceProfile.Surname, insuranceProfile.Premium.ToString());
            }
            else
            {
                // Set payment reference for failed transacation;
                var channel = insuranceProfile.InsuranceService == InsuranceProvider.Hygeia.ToString() ? PaymentReference_ChannelValue.healthinsured_hygeia.ToString()
                    : PaymentReference_ChannelValue.healthinsured_axamansard.ToString();

                var paymentReference = new PaymentReference(channel, chargeAuthorization.Reference, insuranceProfile.Id, null, insuranceProfile.UserId
                   , insuranceProfile.Premium, PaymentReference_StatusValue.Failed.ToString());
                _repoWrapper.PaymentReference.Create(paymentReference);
                await _repoWrapper.PaymentReference.Save();

                paymentOnReactivation.Status = PaymentOnReactivation_StatusValue.Failed.ToString(); paymentOnReactivation.Message = chargeAuthorization.Message;
                paymentOnReactivation.PaymentReference = chargeAuthorization.Reference;
                paymentOnReactivation.EnrollmentOnReactivation.Status = EnrollmentOnReactivation_StatusValue.Terminated.ToString(); paymentOnReactivation.EnrollmentOnReactivation.Message = EnrollmentOnReactivation_StatusValue.Terminated.ToString();
                _repoWrapper.PaymentOnReactivation.Update(paymentOnReactivation);

                insuranceProfile.PendingJobId = null; insuranceProfile.SubscriptionStatus = false;
                insuranceProfile.ActiveStatus = false;
                _repoWrapper.InsuranceProfile.Update(insuranceProfile);

                _insuranceSerivce.SendEmailOnFailedDebit(insuranceProfile.Email, insuranceProfile.Surname, insuranceProfile.Premium.ToString());
            }
            await _repoWrapper.Save();
            await Task.CompletedTask;
        }
    } 
}

