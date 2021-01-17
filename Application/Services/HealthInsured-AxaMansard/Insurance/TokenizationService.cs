using Application.API_RequestModel.HealthInsured_AxaMansard;
using Application.API_RequestModel.Paystack;
using Application.API_ResponseModel.Paystack;
using Application.DTO;
using Application.HealthInsured_AxaMansard_Service.AuditAndReport.AuditLog;
using Application.HealthInsured_AxaMansard_Service.Insurance;
using Application.Helpers;
using Application.Interfaces;
using Application.Services.Paystack;
using Application.ViewModels;
using Application.ViewModels.Paystack;
using AutoMapper;
using DataAccess.General.Interfaces;
using DataAccess.HealthInsured_AxaMansard.Interfaces;
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

namespace Application.Services.HealthInsured_AxaMansard.Insurance
{
    public class TokenizationService
    {
        private readonly IAxaMansardUserProfileRepository _mansardUserProfileRepository;
        private readonly IAxaMansardCompletionRepository _completionRepository;
        private readonly ICardRepository _cardRepository;
        private readonly IMapper _mapper;
        private readonly PaystackService _paystackService;
        private readonly AuditLogService _auditLogServices;
        private readonly InsuranceService _insuranceSerivce;
        private readonly IScheduledPaymentRepository _scheduledPayment;
        private readonly IScheduledAxaEnrollmentRepository _scheduledAxaEnrollment;
        private readonly IAxaEnrollmentReactivationRepository _axaEnrollmentOnReactivation;
        private readonly IPaymentOnReactivationRepository _paymentOnReactivation;
        private readonly IEmailSender _emailSender;
        private readonly IAxaEnrollmentOnOnboardingRepository _enrollmentOnOnboardingRepository;
        private readonly IPaymentReferenceRepository _paymentReference;

        private SubscriptionDuration _subscriptionAccessor { get; }

        public TokenizationService(IAxaMansardUserProfileRepository mansardUserProfileRepository,IAxaMansardCompletionRepository completionRepository,
            ICardRepository cardRepository,IMapper mapper,PaystackService paystackService, AuditLogService auditLogServices, InsuranceService insuranceSerivce,
            IOptions<SubscriptionDuration> subscriptionAccessor, IScheduledPaymentRepository scheduledPayment, IScheduledAxaEnrollmentRepository scheduledAxaEnrollment
            ,IAxaEnrollmentReactivationRepository axaEnrollmentOnReactivation, IPaymentOnReactivationRepository paymentOnReactivation,IEmailSender emailSender,
            IAxaEnrollmentOnOnboardingRepository enrollmentOnOnboardingRepository, IPaymentReferenceRepository paymentReference)
        {
            _mansardUserProfileRepository = mansardUserProfileRepository;
            _completionRepository = completionRepository;
            _cardRepository = cardRepository;
            _mapper = mapper;
            _paystackService = paystackService;
            _auditLogServices = auditLogServices;
            _insuranceSerivce = insuranceSerivce;
            _scheduledPayment = scheduledPayment;
            _scheduledAxaEnrollment = scheduledAxaEnrollment;
            _axaEnrollmentOnReactivation = axaEnrollmentOnReactivation;
            _paymentOnReactivation = paymentOnReactivation;
            _emailSender = emailSender;
            _enrollmentOnOnboardingRepository = enrollmentOnOnboardingRepository;
            _paymentReference = paymentReference;
            _subscriptionAccessor = subscriptionAccessor.Value;
        }

        /// <summary>
        /// This Method is when user tokenize their card, and when existing users add new card, so method takes in logic
        /// to process all payments involved in all senario. For a user adding card, Just a fee of 100 naira is deducted to process
        /// their card and get an authorization code to use in future payments. For first time users tokenizing card for the first time,
        /// A fee of 100 naira is used to test the card if their is a free trial and a scheduled job is used for future deductions.
        /// If theres is no free trial, subscription premium fee is paid and a scheduled job is used for future deductions.
        /// </summary>
        /// <param name="chargeCard"></param>
        /// <param name="id"></param>
        /// <param name="ipAddress"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> TokenizeCard(ChargeCardViewModel chargeCard, int id,string ipAddress,string device)
        {
            var userAxamansardProfile = await _mansardUserProfileRepository.GetByUserIdAsync(id);
            var checkprofileComplete = await _completionRepository.GetCompletionStateByUserId(id);
            if (checkprofileComplete != null)
            {
                if (checkprofileComplete.ProfileCompleted == true)
                {
                    // remove empty space from the card.
                    var cardNumber = chargeCard.card.number.Replace(" ", "");

                    // Check if user has card dat matches last four card digit
                    var checkIfCardWasPreviouslyTokenized = await _cardRepository.CheckIfCardWasPreviouslyTokenized(id, cardNumber.Substring(cardNumber.Length - 4));
                    if (checkIfCardWasPreviouslyTokenized != null) return new ResponseMessage { Message = "This card was previously tokenized" };
                     
                    var chargeCardRequest = _mapper.Map<API_RequestModel.Paystack.Card>(chargeCard.card);

                    var card = new ChargeCard();
                    card.card = chargeCardRequest; card.email = userAxamansardProfile.Email; 
                    card.reference = Guid.NewGuid().ToString(); card.pin = chargeCard.pin;

                    // If the user has is neither active nor  deactivated
                    if(userAxamansardProfile.SubscriptionStatus == null)
                    {
                        // if user can be on a free trail do a test charge of 100 naria, we send amount in Kobo
                        if (_subscriptionAccessor.FreeTrial)
                        {
                            card.amount = (100 * 100).ToString();
                        }
                        // if user cant be on free trial do a charge of premium fee, we send amount in kobo
                        else
                        {
                            card.amount = (userAxamansardProfile.Premium * 100).ToString();
                        }
                    }
                    // if the user has an activa or deactivated subscription status i.e userAxamansardProfile.SubscriptionStatus != null , 
                    // we do a test charge of 100 naria, we send amount in Kobo
                    // Since user is just adding a new card.
                    else
                    {
                        card.amount = (100 * 100).ToString();
                    }

                    // Create payment reference for the charge.
                    var paymentReference = new PaymentReference(card.reference, userAxamansardProfile.Id, userAxamansardProfile.UserId
                    , userAxamansardProfile.Premium, "Pending");
                    _paymentReference.Create(paymentReference);
                    await _paymentReference.Save();

                    // Paystack service to charge user card
                    var chargeCardResponse = await _paystackService.ChargeCard(card, id);                    

                    // function to process response from paystack
                    return await ProcessPaystackChargeCardResponse(chargeCardResponse, userAxamansardProfile, checkprofileComplete
                       ,paymentReference, card.reference, ipAddress, device);

                }
                return new ResponseMessage { Message = "User has not been profiled,kindly create your profile", Status = false };
            }
            return new ResponseMessage { Message = "User does not have a profile,kindly create your profile", Status = false };
        }

        public async Task<ResponseMessage> ProcessPaystackChargeCardResponse(TokenizationResponse chargeCardResponse, AxaMansardUserProfile userAxamansardProfile,
           AxaMansardCompletionProfile checkprofileComplete,PaymentReference paymentReference, string cardReference, string ipAddress, string device)
        {           
            // if charge card was successfully
            if (chargeCardResponse.Status == true && chargeCardResponse.ResponseCode == 0)
            {
                //If card count is 0. it means there is no card available, so the card tokenised will
                //be the primary card so primary card status is set to 1 
                //else, card status is 0;
                var cardStatus = userAxamansardProfile.Cards.Count == 0 ? 1 : 0;

                var debitCard = new DebitCard(userAxamansardProfile.UserId, userAxamansardProfile.Id, cardStatus, chargeCardResponse.LastDigit, chargeCardResponse.Type
                    , cardReference, chargeCardResponse.AuthorizationCode);
                _cardRepository.Create(debitCard);

                // Check if user does not have a subscrption status. That User is tokenizing card for the first time.
                if (userAxamansardProfile.SubscriptionStatus == null)
                {
                    //send user details to axamnasard when payment is successfully
                    await EnrollUserToAxamansardOnOnboarding(userAxamansardProfile);

                    // Schedule Payment for user tokenizing card for the first time.
                    var getScheduledPaymentJobId = await ProcessScheduledPayment(userAxamansardProfile);

                    // Schedule debit email reminder for user 
                    var emailReminderJobId = BackgroundJob.Schedule(() => SendEmailReminder(userAxamansardProfile.Email, userAxamansardProfile.Surname, null),
                        DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration).Subtract(new TimeSpan(3, 0, 0, 0)));

                    // Save scheduled debit job Id
                    userAxamansardProfile.PendingJobId = getScheduledPaymentJobId;
                    //Save scheduled email jobId
                    userAxamansardProfile.PendingEmailJobId = emailReminderJobId;

                    userAxamansardProfile.SubscriptionStatus = true;
                    userAxamansardProfile.ActiveStatus = true;
                    userAxamansardProfile.StartActiveStatusDate = DateTime.Now;
                    userAxamansardProfile.EndActiveStatusDate = DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration);

                    checkprofileComplete.TokenizationCompleted = true;
                    _completionRepository.Update(checkprofileComplete);

                    _mansardUserProfileRepository.Update(userAxamansardProfile);

                    SendSuccesfulSubscriptionMail(userAxamansardProfile.Email, userAxamansardProfile.Surname, userAxamansardProfile.TransId, userAxamansardProfile.CareProviderName);

                    //Create Audit thats user subscrption changed 
                    var auditViewModel2 = new AuditLogViewModel(userAxamansardProfile.UserId, null, "Inactive subscription status", "Subscription Status Changed",
                        "Active subscription status");
                    await _auditLogServices.UserCreateAuditLog(auditViewModel2, ipAddress, device);
                }

                await _mansardUserProfileRepository.Save();

                var auditViewModel = new AuditLogViewModel(userAxamansardProfile.UserId, null, null, "Debit Card Added", null);
                await _auditLogServices.UserCreateAuditLog(auditViewModel, ipAddress, device);


                return new ResponseMessage
                {
                    Data = chargeCardResponse,
                    Status = chargeCardResponse.Status,
                    ResponseCode = chargeCardResponse.ResponseCode,
                    Message = chargeCardResponse.Message
                };
            }
            // If charge card response request for OTP
            else if (chargeCardResponse.Status == true && chargeCardResponse.ResponseCode == 12)
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

        public async Task EnrollUserToAxamansardOnOnboarding(AxaMansardUserProfile userAxamansardProfile)
        {
            // Send user details to axamansard
            var enrollmentModel = _mapper.Map<EnrollmentModel>(userAxamansardProfile);
            var enrollment = await _insuranceSerivce.EnrollUser(enrollmentModel);
            if (!enrollment.Status)
            {
                var axaEnrollmentOnOnboarding = new AxaEnrollmentOnOnboarding(userAxamansardProfile.UserId, userAxamansardProfile.Id, null, "Failed", enrollment.Message);
                _enrollmentOnOnboardingRepository.Create(axaEnrollmentOnOnboarding);
                await _enrollmentOnOnboardingRepository.Save();
            }
            await Task.CompletedTask;
        }

        public async Task<string> ProcessScheduledPayment(AxaMansardUserProfile userAxamansardProfile)
        {
            var executionDate = DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration);

            var jobId = BackgroundJob.Schedule(() => SchedulePaymentLogic(userAxamansardProfile.UserId,
                 userAxamansardProfile.Id, null), executionDate);

            var processingAxaEnrollment = new ScheduledAxaEnrollment(userAxamansardProfile.UserId, userAxamansardProfile.Id, executionDate, jobId, "Processing", null);
            _scheduledAxaEnrollment.Create(processingAxaEnrollment);

            var processingScheduledPayment = new ScheduledPayment(userAxamansardProfile.UserId, userAxamansardProfile.Id, processingAxaEnrollment.Id,
                executionDate, jobId, "Processing",null,null);
            _scheduledPayment.Create(processingScheduledPayment);
            await _scheduledPayment.Save();

            return jobId;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task SchedulePaymentLogic(int userId, int axamansardUserId, PerformContext context)
        {
            var jobId = context.BackgroundJob.Id;
            var axaMansardProfile = await _mansardUserProfileRepository.GetByUserIdAsync(userId);
            var activeCard = axaMansardProfile.Cards.FirstOrDefault(x => x.Status == 1);

            //Get a charge authorization model to use in scheduled payment background process.
            var chageAuthorizationModel = new ChargeAuthorization()
            {
                email = axaMansardProfile.Email,
                amount = (axaMansardProfile.Premium * 100).ToString(),
                authorization_code = activeCard.Authorization_Code
            };

            var chargeAuthorization = await _paystackService.ChargeAuthorization(chageAuthorizationModel);
            var scheduledPaymentJob = await _scheduledPayment.GetScheduledPaymentByJobId(jobId);
            if (chargeAuthorization.Status)
            {
                scheduledPaymentJob.Status = "Successful"; scheduledPaymentJob.Message = chargeAuthorization.Message;
                scheduledPaymentJob.PaymentReference = chargeAuthorization.Reference;

                axaMansardProfile.SubscriptionStatus = true; axaMansardProfile.ActiveStatus = true;
                axaMansardProfile.StartActiveStatusDate = DateTime.Now; axaMansardProfile.EndActiveStatusDate = DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration);
                
                var enrollmentModel = _mapper.Map<EnrollmentModel>(axaMansardProfile);
                var enrollment = await _insuranceSerivce.EnrollUser(enrollmentModel);
                if (enrollment.Status)
                {
                    scheduledPaymentJob.ScheduledAxaEnrollment.Status = "Successful"; scheduledPaymentJob.ScheduledAxaEnrollment.Message = enrollment.Message;
                }
                else
                {
                    scheduledPaymentJob.ScheduledAxaEnrollment.Status = "Failed"; scheduledPaymentJob.ScheduledAxaEnrollment.Message = enrollment.Message;
                }
                _scheduledPayment.Update(scheduledPaymentJob);

                var newJobId = await ProcessScheduledPayment(axaMansardProfile);
                axaMansardProfile.PendingJobId = newJobId;
                _mansardUserProfileRepository.Update(axaMansardProfile);

                await _scheduledPayment.Save();
                await Task.CompletedTask;
            }
            // Insufficient funds
            else if (!chargeAuthorization.Status && chargeAuthorization.ResponseCode == 10)
            {
                var paymentReference = new PaymentReference(chargeAuthorization.Reference, axaMansardProfile.Id, axaMansardProfile.UserId
                    , axaMansardProfile.Premium, "Failed");
                _paymentReference.Create(paymentReference);

                scheduledPaymentJob.Status = "Terminated"; scheduledPaymentJob.Message = chargeAuthorization.Message;

                scheduledPaymentJob.ScheduledAxaEnrollment.Status = "Terminated"; scheduledPaymentJob.ScheduledAxaEnrollment.Message = "Terminated";
                scheduledPaymentJob.PaymentReference = chargeAuthorization.Reference;

                axaMansardProfile.PendingJobId = null; axaMansardProfile.SubscriptionStatus = false;
                axaMansardProfile.ActiveStatus = false;
                _mansardUserProfileRepository.Update(axaMansardProfile);
                await _mansardUserProfileRepository.Save();
                BackgroundJob.Delete(jobId);
                await Task.CompletedTask;
            }
            // Failed
            else
            {
                var paymentReference = new PaymentReference(chargeAuthorization.Reference, axaMansardProfile.Id, axaMansardProfile.UserId
                   , axaMansardProfile.Premium, "Failed");
                _paymentReference.Create(paymentReference);

                scheduledPaymentJob.Status = "Failed"; scheduledPaymentJob.Message = chargeAuthorization.Message;
                scheduledPaymentJob.ScheduledAxaEnrollment.Status = "Failed"; scheduledPaymentJob.ScheduledAxaEnrollment.Message = "Failed";
                scheduledPaymentJob.PaymentReference = chargeAuthorization.Reference;
                _scheduledPayment.Update(scheduledPaymentJob);

                axaMansardProfile.PendingJobId = null; axaMansardProfile.SubscriptionStatus = false;
                axaMansardProfile.ActiveStatus = false;
                _mansardUserProfileRepository.Update(axaMansardProfile);
                await _mansardUserProfileRepository.Save();
                BackgroundJob.Delete(jobId);
                await Task.CompletedTask;
            }
            await Task.CompletedTask;
        }

        public async Task<ResponseMessage> SubmitOtp(SetOtpViewModel otpViewModel,int id, string ipAddress, string device)
        {
            var userAxamansardProfile = await _mansardUserProfileRepository.GetByUserIdAsync(id);
            var checkprofileComplete = await _completionRepository.GetCompletionStateByUserId(id);
            var paymentReference = await _paymentReference.GetByReference(otpViewModel.reference);
            if (checkprofileComplete != null)
            {
                if (checkprofileComplete.ProfileCompleted == true)
                {
                    var chargeCardResponse = await _paystackService.SendOtp(otpViewModel.otp, otpViewModel.reference, userAxamansardProfile.PhoneNumber
                        , userAxamansardProfile.DateOfBirth,otpViewModel.pin);
                    return await ProcessPaystackChargeCardResponse(chargeCardResponse, userAxamansardProfile, checkprofileComplete,
                        paymentReference, otpViewModel.reference,ipAddress,device);
                }
                return new ResponseMessage { Message = "User has not been profiled,kindly create your profile", Status = false };
            }
            return new ResponseMessage { Message = "User does not have a profile,kindly create your profile", Status = false };
        }

        /// <summary>
        /// Service to cancel user subcription
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> CancelSubscription(int userId,string reason)
        {
            var axamansardprofile = await _mansardUserProfileRepository.GetByUserIdAsync(userId);
            if (axamansardprofile.SubscriptionStatus == false)
            {
                return new ResponseMessage { Message = "You have no active subscription", Status = false };
            }

            var scheduledJobId = axamansardprofile.PendingJobId;

            // Check if user has a pending scheduled debit
            var scheduledPayment = await _scheduledPayment.GetScheduledPaymentByJobId(scheduledJobId);   
            if(scheduledPayment != null)
            {
                BackgroundJob.Delete(scheduledJobId);
                if(axamansardprofile.PendingEmailJobId != null)
                {
                    BackgroundJob.Delete(axamansardprofile.PendingEmailJobId);
                }
                scheduledPayment.Status = "Cancelled"; scheduledPayment.Message = "Cancelled";
                scheduledPayment.ScheduledAxaEnrollment.Status = "Cancelled"; scheduledPayment.ScheduledAxaEnrollment.Message = "Cancelled";
                _scheduledPayment.Update(scheduledPayment);
            }
            else
            {
                //Check if a user has a pending debit on scheduled subscription reactivation
                var scheduledReactivatedPayment = await _paymentOnReactivation.GetScheduledPaymentByJobId(scheduledJobId,axamansardprofile.UserId);
                if(scheduledReactivatedPayment != null)
                {
                    BackgroundJob.Delete(scheduledJobId);
                    if (axamansardprofile.PendingEmailJobId != null)
                    {
                        BackgroundJob.Delete(axamansardprofile.PendingEmailJobId);
                    }
                    scheduledReactivatedPayment.Status = "Cancelled"; scheduledReactivatedPayment.Message = "Cancelled";
                    scheduledReactivatedPayment.AxaEnrollmentOnReactivation.Status = "Cancelled";
                    scheduledReactivatedPayment.AxaEnrollmentOnReactivation.Message = "Cancelled";
                    _paymentOnReactivation.Update(scheduledReactivatedPayment);
                }
            }            

            var daysToCancelUserActivityStatus = axamansardprofile.EndActiveStatusDate;
            // check if date active cycle will end correspond with present date. if so set active cycle to false.
            if(daysToCancelUserActivityStatus.Date == DateTime.Now.Date)
            {
                axamansardprofile.ActiveStatus = false;
                axamansardprofile.PendingJobId = null;
                axamansardprofile.PendingEmailJobId = null;
            }
            else
            {
                // Schedule task to render user status inactive when cycle ends
                var jobId = BackgroundJob.Schedule(() => ProcessUserActiveStatusCancellation(axamansardprofile.UserId), daysToCancelUserActivityStatus);
                axamansardprofile.PendingJobId = jobId;
                axamansardprofile.PendingEmailJobId = null;
            }

            axamansardprofile.SubscriptionStatus = false;
            _mansardUserProfileRepository.Update(axamansardprofile);
            await _scheduledPayment.Save();
            var profileDTO = _mapper.Map<AxaMansardUserDTO>(axamansardprofile);
            return new ResponseMessage { Data = profileDTO, Message = "Subscription was canceled successfully.However you remain active till " +
                "your insurance cycle ends.", Status = true };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task ProcessUserActiveStatusCancellation(int userId)
        {
            var axamansardprofile = await _mansardUserProfileRepository.GetByUserIdAsync(userId);
            axamansardprofile.ActiveStatus = false;
            _mansardUserProfileRepository.Update(axamansardprofile);
            await _mansardUserProfileRepository.Save();
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
            var userAxamansardProfile = await _mansardUserProfileRepository.GetByUserIdAsync(userId);

            if (card == null) return new ResponseMessage { Message = "Card  was not found", ResponseCode=12 };
            if (card.Status == 1 && userAxamansardProfile.SubscriptionStatus == true) return new ResponseMessage { Message = "Kindly set a new card as" +
                "primary card to delete present primary card" };

            _cardRepository.Delete(card);
            await _cardRepository.Save();

            var auditViewModel3 = new AuditLogViewModel(userId, null, $"Card id is ${cardId}", "Delete Card", $"Card was deleted successfully");
            BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel3, ipAddress, device));
            return new ResponseMessage { Message = "Card was deleted successfully", Status = true };
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
            var userAxamansardProfile = await _mansardUserProfileRepository.GetByUserIdAsync(userId);
            if (userAxamansardProfile.SubscriptionStatus == true)
            {
                return new ResponseMessage { Message = "Subscription is currently active", Status = false };
            }
            var primaryCard = userAxamansardProfile.Cards.FirstOrDefault(x => x.Status == 1);
            if (primaryCard == null)
            {
                return new ResponseMessage { Message = "Kindly add a primary card, then start the reactivation process" };
            }

            // Check is user is in an active cycle
            if (userAxamansardProfile.ActiveStatus == false)
            {
                return await ProcessReactivationFlow(userAxamansardProfile, primaryCard.Authorization_Code,null);
            }
            else
            {
                var pendingReactivation= await ProcessReactivationFlow(userAxamansardProfile, primaryCard.Authorization_Code
                    , userAxamansardProfile.EndActiveStatusDate);
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
        private async Task<ResponseMessage> ProcessReactivationFlow(AxaMansardUserProfile userAxamansardProfile, string authorization_Code,DateTime? reactivationTime)
        {            
            if (reactivationTime is null)
            {   
                // Process immeediate reactivation
                return await ProcessImmediateReactivationPayment(userAxamansardProfile, authorization_Code);
            }
            else
            {
                // If user is still in active cycle and want to reactivate, that means user must have a background job scheduled to render user INACTIVE
                // at the end of current cycle.
                // To resolve this, we delete background task scheduled to render user inactive at the end of current cycle.
                // Background Task is ProcessUserActiveStatusCancellation            
                BackgroundJob.Delete(userAxamansardProfile.PendingJobId);

                // Scheduled Subscription Reactivation
                var jobId = BackgroundJob.Schedule(() => ProcessScheduledReactivationPayment(userAxamansardProfile.UserId, authorization_Code), reactivationTime.Value);

                // Set Axa enrollment and payment to processing. Check Model to see wat data is used for
                var axaEnrollment = new AxaEnrollmentOnReactivation(userAxamansardProfile.UserId, userAxamansardProfile.Id, reactivationTime.Value, "Processing",
                   null);
                _axaEnrollmentOnReactivation.Create(axaEnrollment);
                await _axaEnrollmentOnReactivation.Save();

                var paymentOnReactivation = new PaymentOnReactivation(userAxamansardProfile.UserId, userAxamansardProfile.Id, axaEnrollment.Id, reactivationTime.Value
                    , "Processing", jobId, null,null);
                _paymentOnReactivation.Create(paymentOnReactivation);

                userAxamansardProfile.SubscriptionStatus = true;
                userAxamansardProfile.ActiveStatus = true;
                userAxamansardProfile.PendingJobId = jobId;
                await _axaEnrollmentOnReactivation.Save();
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
        public async Task<ResponseMessage> ProcessImmediateReactivationPayment(AxaMansardUserProfile userAxamansardProfile, string authorization_Code)
        {
            var chageAuthorizationModel = new ChargeAuthorization()
            {
                email = userAxamansardProfile.Email,
                amount = (userAxamansardProfile.Premium * 100).ToString(),
                authorization_code = authorization_Code
            };

            // Call Paystack service. Debit user using card authorization code
            var chargeAuthorization = await _paystackService.ChargeAuthorization(chageAuthorizationModel);
            if (chargeAuthorization.Status)
            {  
                //Create data to track user axa mansard enrollment and payment
                var immediateAxaEnrollment = new AxaEnrollmentOnReactivation(userAxamansardProfile.UserId, userAxamansardProfile.Id, DateTime.Now, "Processing", null);
                _axaEnrollmentOnReactivation.Create(immediateAxaEnrollment);
                await _axaEnrollmentOnReactivation.Save();

                var enrollmentModel = _mapper.Map<EnrollmentModel>(userAxamansardProfile);
                // Send user details to axamansard
                var enrollment = await _insuranceSerivce.EnrollUser(enrollmentModel);

                if (enrollment.Status)
                {
                    immediateAxaEnrollment.Status = "Successful"; immediateAxaEnrollment.Message = enrollment.Message;
                    _axaEnrollmentOnReactivation.Update(immediateAxaEnrollment);
                }
                else
                {
                    immediateAxaEnrollment.Status = "Failed"; immediateAxaEnrollment.Message = enrollment.Message;
                    _axaEnrollmentOnReactivation.Update(immediateAxaEnrollment);
                }               

                var immediatePaymentOnReactivation = new PaymentOnReactivation(userAxamansardProfile.UserId, userAxamansardProfile.Id, immediateAxaEnrollment.Id,
                 DateTime.Now, "Successful",null, chargeAuthorization.Message,chargeAuthorization.Reference);
                _paymentOnReactivation.Create(immediatePaymentOnReactivation);

                //Schedule job to debit user every 28 days
                var getScheduledPaymentJobId = await ProcessScheduledPayment(userAxamansardProfile);

                // Schedule debit email reminder for user 
                var emailReminderJobId = BackgroundJob.Schedule(() => SendEmailReminder(userAxamansardProfile.Email, userAxamansardProfile.Surname, null),
                    DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration).Subtract(new TimeSpan(3, 0, 0, 0)));

                userAxamansardProfile.PendingEmailJobId = emailReminderJobId;
                userAxamansardProfile.SubscriptionStatus = true;userAxamansardProfile.PendingJobId = getScheduledPaymentJobId;
                userAxamansardProfile.ActiveStatus = true; userAxamansardProfile.StartActiveStatusDate = DateTime.Now;
                userAxamansardProfile.EndActiveStatusDate = DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration);
                _mansardUserProfileRepository.Update(userAxamansardProfile);

                await _mansardUserProfileRepository.Save();
                return new ResponseMessage { Message = "Reactivation was successful." , Status = true, ResponseCode = chargeAuthorization.ResponseCode };
            }
            // Set payment reference for transacation;
            var paymentReference = new PaymentReference(chargeAuthorization.Reference, userAxamansardProfile.Id, userAxamansardProfile.UserId
                   , userAxamansardProfile.Premium,"Failed");
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
            var userAxamansardProfile = await _mansardUserProfileRepository.GetByUserIdAsync(userId);

            var chageAuthorizationModel = new ChargeAuthorization()
            {
                email = userAxamansardProfile.Email,
                amount = (userAxamansardProfile.Premium * 100).ToString(),
                authorization_code = authorization_Code
            };

            // Call Paystack service. Debit user using card authorization code
            var chargeAuthorization = await _paystackService.ChargeAuthorization(chageAuthorizationModel);

            // Get  payment on reactivation data. 
            var paymentOnReactivation = await _paymentOnReactivation.GetScheduledPaymentByJobId(userAxamansardProfile.PendingJobId,userAxamansardProfile.UserId);
            if (chargeAuthorization.Status)
            {  
                paymentOnReactivation.Status = "Successful"; paymentOnReactivation.Message = chargeAuthorization.Message;
                paymentOnReactivation.PaymentReference = chargeAuthorization.Reference;
                _paymentOnReactivation.Update(paymentOnReactivation);

                var enrollmentModel = _mapper.Map<EnrollmentModel>(userAxamansardProfile);

                // Send user details to axamansard
                var enrollment = await _insuranceSerivce.EnrollUser(enrollmentModel);

                if (enrollment.Status)
                {
                    paymentOnReactivation.AxaEnrollmentOnReactivation.Status="Successful"; paymentOnReactivation.AxaEnrollmentOnReactivation.Message=enrollment.Message;
                }
                else
                {
                    paymentOnReactivation.AxaEnrollmentOnReactivation.Status = "Failed"; paymentOnReactivation.AxaEnrollmentOnReactivation.Message = enrollment.Message;
                }
                _paymentOnReactivation.Update(paymentOnReactivation);

                //Schedule job to debit user every 28 days
                var getScheduledPaymentJobId = await ProcessScheduledPayment(userAxamansardProfile);

                // Schedule debit email reminder for user 
                var emailReminderJobId = BackgroundJob.Schedule(() => SendEmailReminder(userAxamansardProfile.Email, userAxamansardProfile.Surname, null),
                    DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration).Subtract(new TimeSpan(3, 0, 0, 0)));
                

                userAxamansardProfile.PendingEmailJobId = emailReminderJobId;
                userAxamansardProfile.PendingJobId = getScheduledPaymentJobId; userAxamansardProfile.StartActiveStatusDate = DateTime.Now;
                userAxamansardProfile.SubscriptionStatus = true; userAxamansardProfile.ActiveStatus = true;
                userAxamansardProfile.EndActiveStatusDate = DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration);
                _mansardUserProfileRepository.Update(userAxamansardProfile);
            }
            // if user has insufficient funds during charge process
            else if (!chargeAuthorization.Status && chargeAuthorization.ResponseCode == 10)
            {
                // Set payment reference for failed transacation;
                var paymentReference = new PaymentReference(chargeAuthorization.Reference, userAxamansardProfile.Id, userAxamansardProfile.UserId
                   , userAxamansardProfile.Premium, "Failed");
                _paymentReference.Create(paymentReference);
                await _paymentReference.Save();

                paymentOnReactivation.Status = "Terminated"; paymentOnReactivation.Message = chargeAuthorization.Message;
                paymentOnReactivation.AxaEnrollmentOnReactivation.Status = "Terminated"; paymentOnReactivation.AxaEnrollmentOnReactivation.Message = "Terminated";
                _paymentOnReactivation.Update(paymentOnReactivation);

                userAxamansardProfile.PendingJobId = null; userAxamansardProfile.SubscriptionStatus = false;
                userAxamansardProfile.ActiveStatus = false;
                _mansardUserProfileRepository.Update(userAxamansardProfile);
            }
            else
            {
                // Set payment reference for failed transacation;
                var paymentReference = new PaymentReference(chargeAuthorization.Reference, userAxamansardProfile.Id, userAxamansardProfile.UserId
                   , userAxamansardProfile.Premium, "Failed");
                _paymentReference.Create(paymentReference);
                await _paymentReference.Save();

                paymentOnReactivation.Status = "Failed"; paymentOnReactivation.Message = chargeAuthorization.Message;
                paymentOnReactivation.AxaEnrollmentOnReactivation.Status = "Terminated"; paymentOnReactivation.AxaEnrollmentOnReactivation.Message = "Terminated";
                _paymentOnReactivation.Update(paymentOnReactivation);

                userAxamansardProfile.PendingJobId = null; userAxamansardProfile.SubscriptionStatus = false;
                userAxamansardProfile.ActiveStatus = false; 
                _mansardUserProfileRepository.Update(userAxamansardProfile);
            }
            await _mansardUserProfileRepository.Save();
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
        public async Task ProcessPaystackWebHook(string @event, string email, string reference, string authorization_code, string last4, string card_type,string ipAddress)
        {
            if (@event == "charge.success")
            {
                var userAxamansardProfile = await _mansardUserProfileRepository.GetByEmail(email);
                if (userAxamansardProfile != null)
                {
                    var paymentReference = await _paymentReference.GetByReference(reference);
                    if (paymentReference != null)
                    {
                        if (paymentReference.Status == "Send_Url")
                        {
                            var checkprofileComplete = await _completionRepository.GetCompletionStateByUserId(userAxamansardProfile.UserId);

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
                            await ProcessPaystackChargeCardResponse(tokenizationResponse, userAxamansardProfile,
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
                        var paymentReference2 = new PaymentReference(reference, userAxamansardProfile.Id, userAxamansardProfile.UserId
                        , userAxamansardProfile.Premium, "Successful");
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
            _emailSender.SendInsurancePaymentReminder(email, userName);
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
            _emailSender.SendSuccessfulSubscriptionMail(email, userName,enroleeNumber, healthCareProvider);
        }
    } 
}

