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
using Domain.Models.AxaMansard_Insurance;
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

namespace Application.Services.HealthInsured
{
    public class TokenizationService
    {
        private readonly IInsuranceProfileRepository _insuranceProfileRepository;
        private readonly IInsuranceCompletionProfileRepository _completionRepository;
        private readonly ICardRepository _cardRepository;
        private readonly IMapper _mapper;
        private readonly PaystackService _paystackService;
        private readonly AuditLogService _auditLogServices;
        private readonly InsuranceService _insuranceSerivce;
        private readonly IScheduledPaymentRepository _scheduledPayment;
        private readonly IScheduledEnrollmentRepository _scheduledEnrollment;
        private readonly IEnrollmentReactivationRepository _enrollmentOnReactivation;
        private readonly IPaymentOnReactivationRepository _paymentOnReactivation;
        private readonly IEmailSender _emailSender;
        private readonly IEnrollmentOnOnboardingRepository _enrollmentOnOnboardingRepository;
        private readonly IPaymentReferenceRepository _paymentReference;
        private readonly ICompanyProfileRepository _companyProfileRepository;
        private readonly IBeneficiaryReviewRepository _beneficiaryReviewRepository;
        private readonly IdentityService _identityService;
        private readonly IRepositoryWrapper _repoWrapper;
        private SubscriptionDuration _subscriptionAccessor { get; }

        public TokenizationService(IInsuranceProfileRepository insuranceProfileRepository, IInsuranceCompletionProfileRepository completionRepository,
            ICardRepository cardRepository, IMapper mapper, PaystackService paystackService, AuditLogService auditLogServices, InsuranceService insuranceSerivce,
            IOptions<SubscriptionDuration> subscriptionAccessor, IScheduledPaymentRepository scheduledPayment, IScheduledEnrollmentRepository scheduledEnrollment
            , IEnrollmentReactivationRepository enrollmentOnReactivation, IPaymentOnReactivationRepository paymentOnReactivation, IEmailSender emailSender,
            IEnrollmentOnOnboardingRepository enrollmentOnOnboardingRepository, IPaymentReferenceRepository paymentReference, IdentityService identityService,
            ICompanyProfileRepository companyProfileRepository, IBeneficiaryReviewRepository beneficiaryReviewRepository, IRepositoryWrapper repoWrapper)
        {
            _insuranceProfileRepository = insuranceProfileRepository;
            _completionRepository = completionRepository;
            _cardRepository = cardRepository;
            _mapper = mapper;
            _paystackService = paystackService;
            _auditLogServices = auditLogServices;
            _insuranceSerivce = insuranceSerivce;
            _scheduledPayment = scheduledPayment;
            _scheduledEnrollment = scheduledEnrollment;
            _enrollmentOnReactivation = enrollmentOnReactivation;
            _paymentOnReactivation = paymentOnReactivation;
            _emailSender = emailSender;
            _enrollmentOnOnboardingRepository = enrollmentOnOnboardingRepository;
            _paymentReference = paymentReference;
            _subscriptionAccessor = subscriptionAccessor.Value;
            _companyProfileRepository = companyProfileRepository;
            _beneficiaryReviewRepository = beneficiaryReviewRepository;
            _identityService = identityService;
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
        public async Task<ResponseMessage> TokenizeCard(ChargeCardViewModel chargeCard, int id,string ipAddress,string device)
        {
            var profileCompletion = await _insuranceSerivce.GetProfileCompletion(id);
            if (profileCompletion.Data.CorporateUser is false)
            {
                return await ProcessIndividualCardTokenization(chargeCard, id, ipAddress, device);
            }
            else if(profileCompletion.Data.CorporateUser is true)
            {
                return await ProcessCorporateCardTokenization(chargeCard, id, ipAddress, device);
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
        private async Task<ResponseMessage> ProcessIndividualCardTokenization(ChargeCardViewModel chargeCard, int id, string ipAddress, string device)
        {
            var insuranceProfile = await _insuranceProfileRepository.GetByUserIdAsync(id);
            var checkprofileComplete = await _completionRepository.GetCompletionStateByUserId(id);
            if (checkprofileComplete.ProfileCompleted == true)
            {
                // remove empty space from the card.
                var cardNumber = chargeCard.card.number.Replace(" ", "");

                // Check if user has card dat matches last four card digit
                var checkIfCardWasPreviouslyTokenized = await _cardRepository.CheckIfCardWasPreviouslyTokenized(id, cardNumber.Substring(cardNumber.Length - 4));
                if (checkIfCardWasPreviouslyTokenized != null) return new ResponseMessage { Message = "This card was previously tokenized" };

                var chargeCardRequest = _mapper.Map<API_RequestModel.Paystack.Card>(chargeCard.card);

                var card = new ChargeCard
                {
                    card = chargeCardRequest,
                    email = insuranceProfile.Email,
                    reference = Guid.NewGuid().ToString(),
                    pin = chargeCard.pin
                };

                // If the user is neither active nor  deactivated. Means user tokenizing for th first time
                if (insuranceProfile.SubscriptionStatus == null)
                {
                    // if user can be on a free trail do a test charge of 100 naria, we send amount in Kobo
                    if (_subscriptionAccessor.FreeTrial)
                    {
                        card.amount = (50 * 100).ToString();
                    }
                    // if user cant be on free trial do a charge of premium fee, we send amount in kobo
                    else
                    {
                        card.amount = (insuranceProfile.Premium * 100).ToString();
                    }
                }
                // If the user is not tokenizing for the first time
                // we do a test charge of 50 naria to get authorization code to use in future transactions, we send amount in Kobo\
                else
                {
                    card.amount = (50 * 100).ToString();
                }

                // Create payment reference for the charge.
                var paymentReference = new PaymentReference(card.reference, insuranceProfile.Id, null, insuranceProfile.UserId
                , insuranceProfile.Premium, "Pending");
                _paymentReference.Create(paymentReference);
                await _paymentReference.Save();

                // Call Paystack service to charge user card
                var chargeCardResponse = await _paystackService.ChargeCard(card, id);

                // function to process response from paystack
                return await ProcessPaystackChargeCardResponse(chargeCardResponse, insuranceProfile, checkprofileComplete
                    , paymentReference, card.reference, ipAddress, device);
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
        private async Task<ResponseMessage> ProcessCorporateCardTokenization(ChargeCardViewModel chargeCard,  int id, string ipAddress
            ,string device)
        {
            var companyProfile = await _companyProfileRepository.GetCompanyProfileByUserId(id);
            if (companyProfile != null)
            {
                if (companyProfile.ProfileCompleted && companyProfile.EmailConfirmed)
                {
                    // remove empty space from the card.
                    var cardNumber = chargeCard.card.number.Replace(" ", "");

                    // Check if user has card dat matches last four card digit
                    var checkIfCardWasPreviouslyTokenized = await _cardRepository.CheckIfCardWasPreviouslyTokenized(id, cardNumber.Substring(cardNumber.Length - 4));
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
                        var beneficiaryReviews = await _beneficiaryReviewRepository.QueryableBeneficiaryReviews(id);
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
                    var paymentReference = new PaymentReference(card.reference, null, companyProfile.Id, id, decimal.Parse(card.amount), "Pending");
                    _paymentReference.Create(paymentReference);
                    await _paymentReference.Save();

                    // Paystack service to charge user card
                    var chargeCardResponse = await _paystackService.ChargeCard(card, id);

                    // function to process response from paystack
                    return await ProcessPaystackChargeCardResponse(chargeCardResponse, companyProfile, paymentReference,card.reference, ipAddress, device);
                }
                return new ResponseMessage { Message = "User has not been profiled,kindly create your profile", Status = false };
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
           InsuranceCompletionProfile checkprofileComplete,PaymentReference paymentReference, string cardReference, string ipAddress, string device)
        {      
            // if charge card was successfully
            if (chargeCardResponse.Status == true && chargeCardResponse.ResponseCode == 0)
            {
                //If card count is 0. it means there is no card available, so the card tokenised will
                //be the primary card so primary card status is set to 1 
                //else, card status is 0;
                var cardStatus = insuranceUserProfile.Cards.Count == 0 ? 1 : 0;

                var debitCard = new DebitCard(insuranceUserProfile.UserId, insuranceUserProfile.Id,null, cardStatus, chargeCardResponse.LastDigit, chargeCardResponse.Type
                    , cardReference, chargeCardResponse.AuthorizationCode);
                _cardRepository.Create(debitCard);

                // Check if user does not have a subscrption status. That User is tokenizing card for the first time.
                if (insuranceUserProfile.SubscriptionStatus == null)
                {
                    //Send user details to insurance provider when payment is successfully
                    if(insuranceUserProfile.InsuranceService.ToLower() == "axamansard")
                    {
                        await _insuranceSerivce.EnrollUserToAxamansardOnOnboarding(insuranceUserProfile);
                    }
                    else
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

                    // Schedule Payment for user tokenizing card for the first time.
                    var getScheduledPaymentJobId = await ProcessScheduledPayment(insuranceUserProfile);

                    // Schedule debit email reminder to user 3 days before scheduled debit
                    var emailReminderJobId = BackgroundJob.Schedule(() => SendEmailReminder(insuranceUserProfile.Email, insuranceUserProfile.Surname, null),
                        DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration).Subtract(new TimeSpan(3, 0, 0, 0)));

                    // Save scheduled debit job Id
                    insuranceUserProfile.PendingJobId = getScheduledPaymentJobId;
                    //Save scheduled email jobId
                    insuranceUserProfile.PendingEmailJobId = emailReminderJobId;

                    insuranceUserProfile.SubscriptionStatus = true;
                    insuranceUserProfile.ActiveStatus = true;
                    insuranceUserProfile.StartActiveStatusDate = DateTime.Now;
                    insuranceUserProfile.EndActiveStatusDate = DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration);

                    checkprofileComplete.TokenizationCompleted = true;
                    _completionRepository.Update(checkprofileComplete);

                    _insuranceProfileRepository.Update(insuranceUserProfile);

                    SendSuccesfulSubscriptionMail(insuranceUserProfile.Email, insuranceUserProfile.Surname, insuranceUserProfile.TransId, insuranceUserProfile.CareProviderName);

                    //Create Audit thats user subscrption changed 
                    var auditViewModel2 = new AuditLogViewModel(insuranceUserProfile.UserId, null, "Inactive subscription status", "Subscription Status Changed",
                        "Active subscription status");
                    await _auditLogServices.UserCreateAuditLog(auditViewModel2, ipAddress, device);
                }
                // Enqueue method to process refund 0f 50 naira test charge
                else
                {
                    BackgroundJob.Enqueue(() => _paystackService.RefundTestCardFunds(cardReference,(50*100).ToString()));
                }
                await _insuranceProfileRepository.Save();

                var auditViewModel = new AuditLogViewModel(insuranceUserProfile.UserId, null, null, "Debit Card Added", null);
                await _auditLogServices.UserCreateAuditLog(auditViewModel, ipAddress, device);


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
           PaymentReference paymentReference,string cardReference, string ipAddress, string device)
        {
            // if charge card was successfully
            if (chargeCardResponse.Status == true && chargeCardResponse.ResponseCode == 0)
            {
                //If card count is 0. it means there is no card available, so the card tokenised will
                //be the primary card so primary card status is set to 1 
                //else, card status is 0;
                var cardStatus = companyProfile.Cards.Count == 0 ? 1 : 0;

                var debitCard = new DebitCard(companyProfile.UserId, null, companyProfile.Id, cardStatus, chargeCardResponse.LastDigit, chargeCardResponse.Type
                    , cardReference, chargeCardResponse.AuthorizationCode);
                _cardRepository.Create(debitCard);

                if (!companyProfile.TokenizationCompleted)
                {
                    //method to create insurance profiles for the beneficiaires.
                    await _insuranceSerivce.CreateInsuranceProfileForCompanyBeneficiaries(companyProfile.Id, null);

                    //Background task to Enroll all users to hygeia.
                    BackgroundJob.Enqueue(() => _insuranceSerivce.OnboardUsersToHygeia(companyProfile.UserId));
                    

                    //Background task to schedule debit at the end of next cycle
                    var getScheduledPaymentJobId = await ProcessScheduledPayment(companyProfile);

                    // Save scheduled debit job Id
                    companyProfile.PendingJobId = getScheduledPaymentJobId;

                    companyProfile.NextPaymentDate = DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration);
                    companyProfile.TokenizationCompleted = true;
                    _repoWrapper.CompanyProfile.Update(companyProfile);
                }
                // Enqueue method to process refund 0f 50 naira test charge
                else
                {
                    BackgroundJob.Enqueue(() => _paystackService.RefundTestCardFunds(cardReference, (50 * 100).ToString()));
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
                paymentReference.Status = "Send_Otp";
                _paymentReference.Update(paymentReference);
                await _paymentReference.Save();
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
                paymentReference.Status = "Send_Url";
                _paymentReference.Update(paymentReference);
                await _paymentReference.Save();
                return new ResponseMessage
                {
                    Message = chargeCardResponse.Message,
                    Status = chargeCardResponse.Status,
                    ResponseCode = chargeCardResponse.ResponseCode,
                    Data = chargeCardResponse
                };
            }
            // If theres is an error
            paymentReference.Status = "Failed";
            _paymentReference.Update(paymentReference);
            await _paymentReference.Save();
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
            var executionDate = DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration);

            var jobId = BackgroundJob.Schedule(() => SchedulePaymentLogic(insuranceProfile.UserId,
                 insuranceProfile.Id, null), executionDate);

            if(insuranceProfile.InsuranceService.ToLower() == "hygeia")
            {
                var processingAxaEnrollment = new ScheduledEnrollment(insuranceProfile.UserId, insuranceProfile.Id, executionDate, jobId, "Processing", null,"hygeia");
                _scheduledEnrollment.Create(processingAxaEnrollment);

                var processingScheduledPayment = new ScheduledPayment(insuranceProfile.UserId, insuranceProfile.Id, processingAxaEnrollment.Id,null,
                executionDate, jobId, "Processing", null, null, "hygeia");
                _scheduledPayment.Create(processingScheduledPayment);
            }
            else
            {
                var processingAxaEnrollment = new ScheduledEnrollment(insuranceProfile.UserId, insuranceProfile.Id, executionDate, jobId, "Processing", null, "axamansard");
                _scheduledEnrollment.Create(processingAxaEnrollment);

                var processingScheduledPayment = new ScheduledPayment(insuranceProfile.UserId, insuranceProfile.Id, processingAxaEnrollment.Id, null,
                executionDate, jobId, "Processing", null, null, "axamansard");
                _scheduledPayment.Create(processingScheduledPayment);
            } 
            await _scheduledPayment.Save();

            return jobId;
        }

        /// <summary>
        /// Overloaded method to process corporate scheduled payment.Create scheduled enrollment and scheduled payment data that is set to the processing stage.
        /// </summary>
        /// <param name="insuranceProfile"></param>
        /// <returns></returns>
        public async Task<string> ProcessScheduledPayment(CompanyProfile companyProfile)
        {
            var executionDate = DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration);

            var jobId = BackgroundJob.Schedule(() => SchedulePaymentLogic(companyProfile.UserId, null), executionDate);

            var processingScheduledPayment = new ScheduledPayment(companyProfile.UserId, null, null, companyProfile.Id, executionDate, jobId, "Processing", null
                , null, "hygeia");
            _scheduledPayment.Create(processingScheduledPayment);
            await _scheduledPayment.Save();
            return jobId;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task SchedulePaymentLogic(int userId, int axamansardUserId, PerformContext context)
        {
            var jobId = context.BackgroundJob.Id;
            var insuranceProfile = await _insuranceProfileRepository.GetByUserIdAsync(userId);
            var activeCard = insuranceProfile.Cards.FirstOrDefault(x => x.Status == 1);

            //Get a charge authorization model to use in scheduled payment background process.
            var chageAuthorizationModel = new ChargeAuthorization()
            {
                email = insuranceProfile.Email,
                amount = (insuranceProfile.Premium * 100).ToString(),
                authorization_code = activeCard.Authorization_Code
            };

            var chargeAuthorization = await _paystackService.ChargeAuthorization(chageAuthorizationModel);
            var scheduledPaymentJob = await _scheduledPayment.GetScheduledPaymentByJobId(jobId);
            if (chargeAuthorization.Status)
            {
                scheduledPaymentJob.Status = "Successful"; scheduledPaymentJob.Message = chargeAuthorization.Message;
                scheduledPaymentJob.PaymentReference = chargeAuthorization.Reference;

                insuranceProfile.SubscriptionStatus = true; insuranceProfile.ActiveStatus = true;
                insuranceProfile.StartActiveStatusDate = DateTime.Now; insuranceProfile.EndActiveStatusDate = DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration);

                if(insuranceProfile.InsuranceService.ToLower() == "axamansard" || insuranceProfile.InsuranceService == null)
                {
                    var enrollmentModel = _mapper.Map<EnrollmentModel>(insuranceProfile);
                    var enrollment = await _insuranceSerivce.AxamansardRegisterUser(enrollmentModel);
                    if (enrollment.Status)
                    {
                        scheduledPaymentJob.ScheduledEnrollment.Status = "Successful"; scheduledPaymentJob.ScheduledEnrollment.Message = enrollment.Message;
                    }
                    else
                    {
                        scheduledPaymentJob.ScheduledEnrollment.Status = "Failed"; scheduledPaymentJob.ScheduledEnrollment.Message = enrollment.Message;
                    }
                    _scheduledPayment.Update(scheduledPaymentJob);
                }
                else
                {
                    scheduledPaymentJob.ScheduledEnrollment.Status = "Successful"; scheduledPaymentJob.ScheduledEnrollment.Message = "Succesful";
                }               

                // Schedule debit email reminder for user 
                var emailReminderJobId = BackgroundJob.Schedule(() => SendEmailReminder(insuranceProfile.Email, insuranceProfile.Surname, null),
                    DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration).Subtract(new TimeSpan(3, 0, 0, 0)));

                var newJobId = await ProcessScheduledPayment(insuranceProfile);
                insuranceProfile.PendingJobId = newJobId; insuranceProfile.PendingEmailJobId = emailReminderJobId;
                _insuranceProfileRepository.Update(insuranceProfile);

                await _scheduledPayment.Save();
                await Task.CompletedTask;
            }
            // Insufficient funds
            else if (!chargeAuthorization.Status && chargeAuthorization.ResponseCode == 10)
            {
                var paymentReference = new PaymentReference(chargeAuthorization.Reference, insuranceProfile.Id,null, insuranceProfile.UserId
                    , insuranceProfile.Premium, "Failed");
                _paymentReference.Create(paymentReference);

                scheduledPaymentJob.Status = "Terminated"; scheduledPaymentJob.Message = chargeAuthorization.Message;
                scheduledPaymentJob.PaymentReference = chargeAuthorization.Reference;

                scheduledPaymentJob.ScheduledEnrollment.Status = "Terminated"; scheduledPaymentJob.ScheduledEnrollment.Message = "Terminated";
                scheduledPaymentJob.PaymentReference = chargeAuthorization.Reference;

                insuranceProfile.PendingJobId = null; insuranceProfile.SubscriptionStatus = false;
                insuranceProfile.ActiveStatus = false;
                _insuranceProfileRepository.Update(insuranceProfile);
                await _insuranceProfileRepository.Save();
                BackgroundJob.Delete(jobId);

                SendEmailOnFailedDebit(insuranceProfile.Email, insuranceProfile.Surname, insuranceProfile.Premium.ToString(), chargeAuthorization.Message);
                await Task.CompletedTask;
            }
            // Failed
            else
            {
                var paymentReference = new PaymentReference(chargeAuthorization.Reference, insuranceProfile.Id,null, insuranceProfile.UserId
                   , insuranceProfile.Premium, "Failed");
                _paymentReference.Create(paymentReference);

                scheduledPaymentJob.Status = "Failed"; scheduledPaymentJob.Message = chargeAuthorization.Message;
                scheduledPaymentJob.ScheduledEnrollment.Status = "Failed"; scheduledPaymentJob.ScheduledEnrollment.Message = "Failed";
                scheduledPaymentJob.PaymentReference = chargeAuthorization.Reference;
                _scheduledPayment.Update(scheduledPaymentJob);

                insuranceProfile.PendingJobId = null; insuranceProfile.SubscriptionStatus = false;
                insuranceProfile.ActiveStatus = false;
                _insuranceProfileRepository.Update(insuranceProfile);
                await _insuranceProfileRepository.Save();
                BackgroundJob.Delete(jobId);

                SendEmailOnFailedDebit(insuranceProfile.Email, insuranceProfile.Surname, insuranceProfile.Premium.ToString(), chargeAuthorization.Message);

                await Task.CompletedTask;
            }
            await Task.CompletedTask;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task SchedulePaymentLogic(int userId, PerformContext context)
        {
            var jobId = context.BackgroundJob.Id;
            var companyProfile = await _companyProfileRepository.GetCompanyProfileByUserId(userId);
            var activeCard = companyProfile.Cards.FirstOrDefault(x => x.Status == 1);

            //Get a charge authorization model to use in scheduled payment background process.
            var chageAuthorizationModel = new ChargeAuthorization()
            {
                email = companyProfile.CompanyEmail,
                amount = (companyProfile.NextCyclePremiumFee * 100).ToString(),
                authorization_code = activeCard.Authorization_Code
            };

            var chargeAuthorization = await _paystackService.ChargeAuthorization(chageAuthorizationModel);
            var scheduledPaymentJob = await _scheduledPayment.GetScheduledPaymentByJobId(jobId);
            if (chargeAuthorization.Status)
            {
                scheduledPaymentJob.Status = "Successful"; scheduledPaymentJob.Message = chargeAuthorization.Message;
                scheduledPaymentJob.PaymentReference = chargeAuthorization.Reference;
                companyProfile.NextPaymentDate = DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration);

                //// Schedule debit email reminder for user 
                //var emailReminderJobId = BackgroundJob.Schedule(() => SendEmailReminder(insuranceProfile.Email, insuranceProfile.Surname, null),
                //    DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration).Subtract(new TimeSpan(3, 0, 0, 0)));

                var newJobId = await ProcessScheduledPayment(companyProfile);
                companyProfile.PendingJobId = newJobId; 
                _companyProfileRepository.Update(companyProfile);
                await _scheduledPayment.Save();
                await Task.CompletedTask;
            }
            await Task.CompletedTask;
        }
        
        public async Task<ResponseMessage> SubmitOtp(SetOtpViewModel otpViewModel,int id, string ipAddress, string device)
        {
            var profileCompletion = await _insuranceSerivce.GetProfileCompletion(id);
            var paymentReference = await _paymentReference.GetByReference(otpViewModel.reference);
            if (profileCompletion.Data.CorporateUser is false)
            {
                var insuranceProfile = await _insuranceProfileRepository.GetByUserIdAsync(id);
                var checkprofileComplete = await _completionRepository.GetCompletionStateByUserId(id);

                var chargeCardResponse = await _paystackService.SendOtp(otpViewModel.otp, otpViewModel.reference, insuranceProfile.PhoneNumber
                       , insuranceProfile.DateOfBirth, otpViewModel.pin);
                return await ProcessPaystackChargeCardResponse(chargeCardResponse, insuranceProfile, checkprofileComplete,
                    paymentReference, otpViewModel.reference, ipAddress, device);
            }
            else if (profileCompletion.Data.CorporateUser is true)
            {
                var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(id);
                var chargeCardResponse = await _paystackService.SendOtp(otpViewModel.otp, otpViewModel.reference, companyProfile.PhoneNumber
                       ,DateTime.Now, otpViewModel.pin);
                return await ProcessPaystackChargeCardResponse(chargeCardResponse, companyProfile, paymentReference, otpViewModel.reference, ipAddress, device);
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
            var insuranceProfile = await _insuranceProfileRepository.GetByUserIdAsync(userId);
            if (insuranceProfile.SubscriptionStatus == false)
            {
                return new ResponseMessage { Message = "User has no active subscription", Status = false };
            }

            var scheduledJobId = insuranceProfile.PendingJobId;

            // Check if user has a pending scheduled debit
            var scheduledPayment = await _scheduledPayment.GetScheduledPaymentByJobId(scheduledJobId);   
            if(scheduledPayment != null)
            {
                BackgroundJob.Delete(scheduledJobId);
                if(insuranceProfile.PendingEmailJobId != null)
                {
                    BackgroundJob.Delete(insuranceProfile.PendingEmailJobId);
                }
                scheduledPayment.Status = "Cancelled"; scheduledPayment.Message = "Cancelled";
                scheduledPayment.ScheduledEnrollment.Status = "Cancelled"; scheduledPayment.ScheduledEnrollment.Message = "Cancelled";
                _scheduledPayment.Update(scheduledPayment);
            }
            else
            {
                //Check if a user has a pending debit on scheduled subscription reactivation
                var scheduledReactivatedPayment = await _paymentOnReactivation.GetScheduledPaymentByJobId(scheduledJobId, insuranceProfile.UserId);
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
                    _paymentOnReactivation.Update(scheduledReactivatedPayment);
                }
            }

            var daysToCancelUserActivityStatus = insuranceProfile.EndActiveStatusDate;
            // check if date active cycle will end correspond with present date. if so set active cycle to false.
            if (daysToCancelUserActivityStatus.Date == DateTime.Now.Date)
            {
                insuranceProfile.ActiveStatus = false;
                insuranceProfile.PendingJobId = null;
                insuranceProfile.PendingEmailJobId = null;
                var response = await _insuranceSerivce.HygeiaDeactivateUser(insuranceProfile.TransId);
            }
            else
            {
                // Schedule task to render user status inactive when cycle ends
                var jobId = BackgroundJob.Schedule(() => ProcessUserActiveStatusCancellation(insuranceProfile.UserId), daysToCancelUserActivityStatus);
                insuranceProfile.PendingJobId = jobId;
                insuranceProfile.PendingEmailJobId = null;
            }

            insuranceProfile.SubscriptionStatus = false;
            _insuranceProfileRepository.Update(insuranceProfile);
            await _scheduledPayment.Save();
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
                    if (insuranceUserProfile.CompanySubscribedStatus.ToLower() == "active")
                    {
                        insuranceUserProfile.CompanySubscribedStatus = "deactivated";
                        insuranceUserProfile.SubscriptionStatus = false;
                        BackgroundJob.Schedule(() => ProcessUserActiveStatusCancellation(insuranceUserProfile.UserId),insuranceUserProfile.EndActiveStatusDate);
                    }
                    _repoWrapper.InsuranceProfile.Update(insuranceUserProfile);
                }
            }
            var nextCyclePremiumFee = companyProfile.InsuranceUserProfiles.Where(x => x.CompanySubscribedStatus.ToLower() == "active").Select(x => x.Premium).Sum();
            companyProfile.NextCyclePremiumFee = nextCyclePremiumFee;
            _repoWrapper.CompanyProfile.Update(companyProfile);
            await _repoWrapper.Save();
            return new ResponseMessage { Data = "Beneficiaries was deactivated successfully,beneficiaries will still remain active till insurance " +
                "cycle ends.", Status = false };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task ProcessUserActiveStatusCancellation(int userId)
        {
            var insuranceProfile = await _insuranceProfileRepository.GetByUserIdAsync(userId);
            if (insuranceProfile.InsuranceService.ToLower() == "hygeia")
            {
                var response = await _insuranceSerivce.HygeiaDeactivateUser(insuranceProfile.TransId);
            }
            insuranceProfile.ActiveStatus = false;
            _insuranceProfileRepository.Update(insuranceProfile);
            await _insuranceProfileRepository.Save();
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
        public async Task<ResponseMessage> ChangePrimaryCard(int newCardId,int userId,string ipAddress,string device)
        {
            var presentPrimaryCard = await _cardRepository.GetPrimaryCard(userId);
            if (presentPrimaryCard == null)
            {
                return new ResponseMessage { Message = "You dont have a debit card, Kindly add one" };
            }

            var newPrimaryCard = await _cardRepository.GetCardByIdAsync(newCardId, userId);
            if (newPrimaryCard == null)
            {
                return new ResponseMessage { Message = "Card was not found", ResponseCode=12 };
            }
            if (newPrimaryCard.Status == 1)
            {
                return new ResponseMessage { Message = "This card is presently the primary card" };
            }
            presentPrimaryCard.Status = 0;
            _cardRepository.Update(presentPrimaryCard);
            newPrimaryCard.Status = 1;
            _cardRepository.Update(newPrimaryCard);
            await _cardRepository.Save();

            var auditViewModel = new AuditLogViewModel(userId, null, $"Primary card ID is {presentPrimaryCard.Id}", "Change Primary Card", $"New primary card ID is {newCardId}");
            await _auditLogServices.UserCreateAuditLog(auditViewModel, ipAddress, device);

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
        public async Task<ResponseMessage> DeleteCard(int cardId,int userId,string ipAddress,string device)
        {
            var card = await _cardRepository.GetCardByIdAsync(cardId, userId);
            var insuranceProfile = await _insuranceProfileRepository.GetByUserIdAsync(userId);

            if (card == null) return new ResponseMessage { Message = "Card  was not found", ResponseCode=12 };
            if (card.Status == 1 && insuranceProfile.SubscriptionStatus == true) return new ResponseMessage { Message = "Kindly set a new card as" +
                "primary card to delete present primary card" };

            _cardRepository.Delete(card);
            await _cardRepository.Save();

            var auditViewModel3 = new AuditLogViewModel(userId, null, $"Card id is ${cardId}", "Delete Card", $"Card was deleted successfully");
            BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel3, ipAddress, device));
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
                var individualInsuranceUser = await _insuranceProfileRepository.GetByUserIdAsync(userId);
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
        public async Task<ResponseMessage> ReactivateWithPresentPrimaryCard(int userId,string ipAddress, string device)
        {
            var insuranceProfile = await _insuranceProfileRepository.GetByUserIdAsync(userId);
            if (insuranceProfile.SubscriptionStatus == true)
            {
                return new ResponseMessage { Message = "Subscription is currently active", Status = false };
            }
            var primaryCard = insuranceProfile.Cards.FirstOrDefault(x => x.Status == 1);
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
                var pendingReactivation= await ProcessReactivationFlow(insuranceProfile, primaryCard.Authorization_Code
                    , insuranceProfile.EndActiveStatusDate);
                return pendingReactivation;
            }
        }

        /// <summary>
        /// Func to process user activation flow.
        /// Determines maybe user is activated immediately or a background job is scheduled for reactivation
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
                // To resolve this, we delete background task scheduled to render user inactive at the end of current cycle.
                // Background Task is ProcessUserActiveStatusCancellation            
                BackgroundJob.Delete(insuranceProfile.PendingJobId);

                // Scheduled Subscription Reactivation
                var jobId = BackgroundJob.Schedule(() => ProcessScheduledReactivationPayment(insuranceProfile.UserId, authorization_Code), reactivationTime.Value);

                if(insuranceProfile.InsuranceService.ToLower() == "hygeia")
                {
                    // Set enrollment and payment to processing. Check Model to see wat data is used for
                    var enrollment = new EnrollmentOnReactivation(insuranceProfile.UserId, insuranceProfile.Id, reactivationTime.Value, "Processing",
                       null,"hygeia");
                    _enrollmentOnReactivation.Create(enrollment);
                    await _enrollmentOnReactivation.Save();

                    var paymentOnReactivation = new PaymentOnReactivation(insuranceProfile.UserId, insuranceProfile.Id, enrollment.Id, reactivationTime.Value
                        , "Processing", jobId, null, null,"hygeia");
                    _paymentOnReactivation.Create(paymentOnReactivation);
                }
                else
                {
                    // Set enrollment and payment to processing. Check Model to see wat data is used for
                    var enrollment = new EnrollmentOnReactivation(insuranceProfile.UserId, insuranceProfile.Id, reactivationTime.Value, "Processing",
                       null, "axamasard");
                    _enrollmentOnReactivation.Create(enrollment);
                    await _enrollmentOnReactivation.Save();

                    var paymentOnReactivation = new PaymentOnReactivation(insuranceProfile.UserId, insuranceProfile.Id, enrollment.Id, reactivationTime.Value
                        , "Processing", jobId, null, null, "axamasard");
                    _paymentOnReactivation.Create(paymentOnReactivation);
                }

                insuranceProfile.SubscriptionStatus = true;
                insuranceProfile.ActiveStatus = true;
                insuranceProfile.PendingJobId = jobId;
                await _enrollmentOnReactivation.Save();
                return new ResponseMessage {Status = true,Message= "Reactivation was successful.You will be debited a the end of your " +
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
                //Create data to track user axa mansard enrollment and payment
                var immediateEnrollment = new EnrollmentOnReactivation(insuranceProfile.UserId, insuranceProfile.Id, DateTime.Now, "Processing", null,null);
                _enrollmentOnReactivation.Create(immediateEnrollment);
                await _enrollmentOnReactivation.Save();

                if(insuranceProfile.InsuranceService.ToLower() == "hygeia")
                {
                    // Send user details to hygeia
                    var registrationModel = _mapper.Map<RegistrationModel>(insuranceProfile);
                    var registration = await _insuranceSerivce.HygeiaRegisterUser(registrationModel);
                    if (registration.Status)
                    {
                        immediateEnrollment.Status = "Successful"; immediateEnrollment.Message = registration.Message; immediateEnrollment.InsuranceService = "hygeia";
                        _enrollmentOnReactivation.Update(immediateEnrollment);
                    }
                    else
                    {
                        immediateEnrollment.Status = "Failed"; immediateEnrollment.Message = registration.Message; immediateEnrollment.InsuranceService = "hygeia";
                        _enrollmentOnReactivation.Update(immediateEnrollment);
                    }
                }
                else
                {
                    var enrollmentModel = _mapper.Map<EnrollmentModel>(insuranceProfile);
                    // Send user details to axamansard
                    var enrollment = await _insuranceSerivce.AxamansardRegisterUser(enrollmentModel);

                    if (enrollment.Status)
                    {
                        immediateEnrollment.Status = "Successful"; immediateEnrollment.Message = enrollment.Message;immediateEnrollment.InsuranceService = "axamansard";
                        _enrollmentOnReactivation.Update(immediateEnrollment);
                    }
                    else
                    {
                        immediateEnrollment.Status = "Failed"; immediateEnrollment.Message = enrollment.Message; immediateEnrollment.InsuranceService = "axamansard";
                        _enrollmentOnReactivation.Update(immediateEnrollment);
                    }
                }        

                var immediatePaymentOnReactivation = new PaymentOnReactivation(insuranceProfile.UserId, insuranceProfile.Id, immediateEnrollment.Id,
                 DateTime.Now, "Successful",null, chargeAuthorization.Message,chargeAuthorization.Reference, immediateEnrollment.InsuranceService);
                _paymentOnReactivation.Create(immediatePaymentOnReactivation);

                //Schedule job to debit user every 28 days
                var getScheduledPaymentJobId = await ProcessScheduledPayment(insuranceProfile);

                // Schedule debit email reminder for user 
                var emailReminderJobId = BackgroundJob.Schedule(() => SendEmailReminder(insuranceProfile.Email, insuranceProfile.Surname, null),
                    DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration).Subtract(new TimeSpan(3, 0, 0, 0)));

                insuranceProfile.PendingEmailJobId = emailReminderJobId;
                insuranceProfile.SubscriptionStatus = true; insuranceProfile.PendingJobId = getScheduledPaymentJobId;
                insuranceProfile.ActiveStatus = true; insuranceProfile.StartActiveStatusDate = DateTime.Now;
                insuranceProfile.EndActiveStatusDate = DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration);
                _insuranceProfileRepository.Update(insuranceProfile);

                await _insuranceProfileRepository.Save();
                return new ResponseMessage { Message = "Reactivation was successful." , Status = true, ResponseCode = chargeAuthorization.ResponseCode };
            }
            // Set payment reference for transacation;
            var paymentReference = new PaymentReference(chargeAuthorization.Reference, insuranceProfile.Id,null, insuranceProfile.UserId
                   , insuranceProfile.Premium,"Failed");
            _paymentReference.Create(paymentReference);
            await _paymentReference.Save();

            return new ResponseMessage { Message = chargeAuthorization.Message, Status =false, ResponseCode = chargeAuthorization.ResponseCode };
        }

        /// <summary>
        /// Background Task Func to process scheduled Reactivation.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="authorization_Code"></param>
        /// <returns></returns>
        [AutomaticRetry(Attempts = 0)]
        public async Task ProcessScheduledReactivationPayment(int userId,string authorization_Code)
        {
            var insuranceProfile = await _insuranceProfileRepository.GetByUserIdAsync(userId);

            var chageAuthorizationModel = new ChargeAuthorization()
            {
                email = insuranceProfile.Email,
                amount = (insuranceProfile.Premium * 100).ToString(),
                authorization_code = authorization_Code
            };

            // Call Paystack service. Debit user using card authorization code
            var chargeAuthorization = await _paystackService.ChargeAuthorization(chageAuthorizationModel);

            // Get  payment on reactivation data. 
            var paymentOnReactivation = await _paymentOnReactivation.GetScheduledPaymentByJobId(insuranceProfile.PendingJobId, insuranceProfile.UserId);
            if (chargeAuthorization.Status)
            {  
                paymentOnReactivation.Status = "Successful"; paymentOnReactivation.Message = chargeAuthorization.Message;
                paymentOnReactivation.PaymentReference = chargeAuthorization.Reference;
                _paymentOnReactivation.Update(paymentOnReactivation);

                if(insuranceProfile.InsuranceService.ToLower() == "hygeia")
                {
                    // Send user details to hygeia
                    var registrationModel = _mapper.Map<RegistrationModel>(insuranceProfile);
                    var registration = await _insuranceSerivce.HygeiaRegisterUser(registrationModel);
                    if (registration.Status)
                    {
                        paymentOnReactivation.EnrollmentOnReactivation.Status = "Successful"; paymentOnReactivation.EnrollmentOnReactivation.Message = registration.Message;                        
                    }
                    else
                    {
                        paymentOnReactivation.EnrollmentOnReactivation.Status = "Failed"; paymentOnReactivation.EnrollmentOnReactivation.Message = registration.Message;
                    }
                }
                else
                {
                    var enrollmentModel = _mapper.Map<EnrollmentModel>(insuranceProfile);

                    // Send user details to axamansard
                    var enrollment = await _insuranceSerivce.AxamansardRegisterUser(enrollmentModel);

                    if (enrollment.Status)
                    {
                        paymentOnReactivation.EnrollmentOnReactivation.Status = "Successful"; paymentOnReactivation.EnrollmentOnReactivation.Message = enrollment.Message;
                    }
                    else
                    {
                        paymentOnReactivation.EnrollmentOnReactivation.Status = "Failed"; paymentOnReactivation.EnrollmentOnReactivation.Message = enrollment.Message;
                    }
                }
                _paymentOnReactivation.Update(paymentOnReactivation);

                //Schedule job to debit user every 28 days
                var getScheduledPaymentJobId = await ProcessScheduledPayment(insuranceProfile);

                // Schedule debit email reminder for user 
                var emailReminderJobId = BackgroundJob.Schedule(() => SendEmailReminder(insuranceProfile.Email, insuranceProfile.Surname, null),
                    DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration).Subtract(new TimeSpan(3, 0, 0, 0)));

                insuranceProfile.PendingEmailJobId = emailReminderJobId;
                insuranceProfile.PendingJobId = getScheduledPaymentJobId; insuranceProfile.StartActiveStatusDate = DateTime.Now;
                insuranceProfile.SubscriptionStatus = true; insuranceProfile.ActiveStatus = true;
                insuranceProfile.EndActiveStatusDate = DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration);
                _insuranceProfileRepository.Update(insuranceProfile);
            }
            // if user has insufficient funds during charge process
            else if (!chargeAuthorization.Status && chargeAuthorization.ResponseCode == 10)
            {
                // Set payment reference for failed transacation;
                var paymentReference = new PaymentReference(chargeAuthorization.Reference, insuranceProfile.Id,null, insuranceProfile.UserId
                   , insuranceProfile.Premium, "Failed");
                _paymentReference.Create(paymentReference);
                await _paymentReference.Save();

                paymentOnReactivation.Status = "Terminated"; paymentOnReactivation.Message = chargeAuthorization.Message;
                paymentOnReactivation.PaymentReference = chargeAuthorization.Reference;
                paymentOnReactivation.EnrollmentOnReactivation.Status = "Terminated"; paymentOnReactivation.EnrollmentOnReactivation.Message = "Terminated";
                _paymentOnReactivation.Update(paymentOnReactivation);

                insuranceProfile.PendingJobId = null; insuranceProfile.SubscriptionStatus = false;
                insuranceProfile.ActiveStatus = false;
                _insuranceProfileRepository.Update(insuranceProfile);

                SendEmailOnFailedDebit(insuranceProfile.Email, insuranceProfile.Surname, insuranceProfile.Premium.ToString(), chargeAuthorization.Message);
            }
            else
            {
                // Set payment reference for failed transacation;
                var paymentReference = new PaymentReference(chargeAuthorization.Reference, insuranceProfile.Id,null, insuranceProfile.UserId
                   , insuranceProfile.Premium, "Failed");
                _paymentReference.Create(paymentReference);
                await _paymentReference.Save();

                paymentOnReactivation.Status = "Failed"; paymentOnReactivation.Message = chargeAuthorization.Message;
                paymentOnReactivation.PaymentReference = chargeAuthorization.Reference;
                paymentOnReactivation.EnrollmentOnReactivation.Status = "Terminated"; paymentOnReactivation.EnrollmentOnReactivation.Message = "Terminated";
                _paymentOnReactivation.Update(paymentOnReactivation);

                insuranceProfile.PendingJobId = null; insuranceProfile.SubscriptionStatus = false;
                insuranceProfile.ActiveStatus = false; 
                _insuranceProfileRepository.Update(insuranceProfile);

                SendEmailOnFailedDebit(insuranceProfile.Email, insuranceProfile.Surname, insuranceProfile.Premium.ToString(), chargeAuthorization.Message);
            }
            await _insuranceProfileRepository.Save();
            await Task.CompletedTask;
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
        public async Task ProcessPaystackWebHook(string @event, string email, string reference, string authorization_code, string last4, string card_type,string ipAddress,string amount)
        {
            if (@event == "charge.success")
            {
                var insuranceProfile = await _insuranceProfileRepository.GetByEmail(email);
                if (insuranceProfile != null)
                {
                    var paymentReference = await _paymentReference.GetByReference(reference);
                    if (paymentReference != null)
                    {
                        if (paymentReference.Status == "Send_Url")
                        {
                            var checkprofileComplete = await _completionRepository.GetCompletionStateByUserId(insuranceProfile.UserId);

                            var tokenizationResponse = new TokenizationResponse
                            {
                                Type = card_type,
                                LastDigit = last4,
                                AuthorizationCode = authorization_code,
                                Message = "Card was tokenize successfully",
                                Status = true,
                                Reference = paymentReference.Refernce,
                                ResponseCode = 0
                            };
                            await ProcessPaystackChargeCardResponse(tokenizationResponse, insuranceProfile,
                                checkprofileComplete, paymentReference, paymentReference.Refernce, ipAddress, "nil");
                            paymentReference.Status = "Successful";
                            _paymentReference.Update(paymentReference);
                            await _paymentReference.Save();
                        }
                        else
                        {
                            paymentReference.Status = "Successful";
                            _paymentReference.Update(paymentReference);
                            await _paymentReference.Save();
                        }
                    }
                    else
                    {
                        // Create payment reference for the charge.
                        var paymentReference2 = new PaymentReference(reference, insuranceProfile.Id,null, insuranceProfile.UserId
                        , decimal.Parse(amount), "Successful");
                        _paymentReference.Create(paymentReference2);
                        await _paymentReference.Save();
                    }
                }
            }
            await Task.CompletedTask;
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

        /// <summary>
        /// Function to send successful subscription email.Send this only to user when subscription status is null
        /// </summary>
        /// <param name="email"></param>
        /// <param name="userName"></param>
        /// <param name="enroleeNumber"></param>
        /// <param name="healthCareProvider"></param>
        public void SendSuccesfulSubscriptionMail(string email, string userName,string enroleeNumber,string healthCareProvider)
        {
            _emailSender.HealthInsuredSubscriptionMail(email,"Active Free Trial",userName,enroleeNumber,healthCareProvider);
        }

        public void SendEmailOnFailedDebit(string email,string userName,string premium,string reason)
        {
            _emailSender.HealthInsuredFailedDebit(email, "Failed Transaction", userName, premium);
        }
    } 
}

