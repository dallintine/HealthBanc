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

        private SubscriptionDuration _subscriptionAccessor { get; }

        public TokenizationService(IInsuranceProfileRepository insuranceProfileRepository,IInsuranceCompletionProfileRepository completionRepository,
            ICardRepository cardRepository,IMapper mapper,PaystackService paystackService, AuditLogService auditLogServices, InsuranceService insuranceSerivce,
            IOptions<SubscriptionDuration> subscriptionAccessor, IScheduledPaymentRepository scheduledPayment, IScheduledEnrollmentRepository scheduledEnrollment
            ,IEnrollmentReactivationRepository enrollmentOnReactivation, IPaymentOnReactivationRepository paymentOnReactivation,IEmailSender emailSender,
            IEnrollmentOnOnboardingRepository enrollmentOnOnboardingRepository, IPaymentReferenceRepository paymentReference)
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
            var insuranceProfile = await _insuranceProfileRepository.GetByUserIdAsync(id);
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
                    card.card = chargeCardRequest; card.email = insuranceProfile.Email; 
                    card.reference = Guid.NewGuid().ToString(); card.pin = chargeCard.pin;

                    // If the user has is neither active nor  deactivated
                    if(insuranceProfile.SubscriptionStatus == null)
                    {
                        // if user can be on a free trail do a test charge of 100 naria, we send amount in Kobo
                        if (_subscriptionAccessor.FreeTrial)
                        {
                            card.amount = (100 * 100).ToString();
                        }
                        // if user cant be on free trial do a charge of premium fee, we send amount in kobo
                        else
                        {
                            card.amount = (insuranceProfile.Premium * 100).ToString();
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
                    var paymentReference = new PaymentReference(card.reference, insuranceProfile.Id,null, insuranceProfile.UserId
                    , insuranceProfile.Premium, "Pending");
                    _paymentReference.Create(paymentReference);
                    await _paymentReference.Save();

                    // Paystack service to charge user card
                    var chargeCardResponse = await _paystackService.ChargeCard(card, id);                    

                    // function to process response from paystack
                    return await ProcessPaystackChargeCardResponse(chargeCardResponse, insuranceProfile, checkprofileComplete
                       ,paymentReference, card.reference, ipAddress, device);

                }
                return new ResponseMessage { Message = "User has not been profiled,kindly create your profile", Status = false };
            }
            return new ResponseMessage { Message = "User does not have a profile,kindly create your profile", Status = false };
        }

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
                    //send user details to insurance provider when payment is successfully
                    if(insuranceUserProfile.InsuranceService == "AxaMansard")
                    {
                        await EnrollUserToAxamansardOnOnboarding(insuranceUserProfile);
                    }
                    else
                    {
                        var response = await EnrollUserToHygeiaOnOnboarding(insuranceUserProfile);
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

                    // Schedule debit email reminder for user 
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

        public async Task EnrollUserToAxamansardOnOnboarding(InsuranceUserProfile insuranceUserProfile)
        {
            // Send user details to axamansard
            var enrollmentModel = _mapper.Map<EnrollmentModel>(insuranceUserProfile);
            var enrollment = await _insuranceSerivce.AxamansardRegisterUser(enrollmentModel);
            if (!enrollment.Status)
            {
                var axaEnrollmentOnOnboarding = new EnrollmentOnOnboarding(insuranceUserProfile.UserId, insuranceUserProfile.Id, null, "Failed"
                    , enrollment.Message,"Axamansard");
                _enrollmentOnOnboardingRepository.Create(axaEnrollmentOnOnboarding);
                await _enrollmentOnOnboardingRepository.Save();
            }
            await Task.CompletedTask;
        }

        public async Task<ResponseMessage> EnrollUserToHygeiaOnOnboarding(InsuranceUserProfile insuranceUserProfile)
        {
            // Send user details to hygeia
            var registrationModel = _mapper.Map<RegistrationModel>(insuranceUserProfile);
            var registration = await _insuranceSerivce.HygeiaRegisterUser(registrationModel);
            if (!registration.Status)
            {
                var enrollmentOnOnboarding = new EnrollmentOnOnboarding(insuranceUserProfile.UserId, insuranceUserProfile.Id, null, "Failed", registration.Message,
                    "Hygeia");
                _enrollmentOnOnboardingRepository.Create(enrollmentOnOnboarding);
                await _enrollmentOnOnboardingRepository.Save();
                return registration;
            }
            return registration;
        }

        public async Task<string> ProcessScheduledPayment(InsuranceUserProfile insuranceProfile)
        {
            var executionDate = DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration);

            var jobId = BackgroundJob.Schedule(() => SchedulePaymentLogic(insuranceProfile.UserId,
                 insuranceProfile.Id, null), executionDate);

            if(insuranceProfile.InsuranceService == "Hygeia")
            {
                var processingAxaEnrollment = new ScheduledEnrollment(insuranceProfile.UserId, insuranceProfile.Id, executionDate, jobId, "Processing", null,"Hygeia");
                _scheduledEnrollment.Create(processingAxaEnrollment);

                var processingScheduledPayment = new ScheduledPayment(insuranceProfile.UserId, insuranceProfile.Id, processingAxaEnrollment.Id,null,
                executionDate, jobId, "Processing", null, null, "Hygeia");
                _scheduledPayment.Create(processingScheduledPayment);
            }
            else
            {
                var processingAxaEnrollment = new ScheduledEnrollment(insuranceProfile.UserId, insuranceProfile.Id, executionDate, jobId, "Processing", null, "Axamansard");
                _scheduledEnrollment.Create(processingAxaEnrollment);

                var processingScheduledPayment = new ScheduledPayment(insuranceProfile.UserId, insuranceProfile.Id, processingAxaEnrollment.Id, null,
                executionDate, jobId, "Processing", null, null, "Axamansard");
                _scheduledPayment.Create(processingScheduledPayment);
            } 
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

                if(insuranceProfile.InsuranceService == "Axamansard" || insuranceProfile.InsuranceService == null)
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

        public async Task<ResponseMessage> SubmitOtp(SetOtpViewModel otpViewModel,int id, string ipAddress, string device)
        {
            var insuranceProfile = await _insuranceProfileRepository.GetByUserIdAsync(id);
            var checkprofileComplete = await _completionRepository.GetCompletionStateByUserId(id);
            var paymentReference = await _paymentReference.GetByReference(otpViewModel.reference);
            if (checkprofileComplete != null)
            {
                if (checkprofileComplete.ProfileCompleted == true)
                {
                    var chargeCardResponse = await _paystackService.SendOtp(otpViewModel.otp, otpViewModel.reference, insuranceProfile.PhoneNumber
                        , insuranceProfile.DateOfBirth,otpViewModel.pin);
                    return await ProcessPaystackChargeCardResponse(chargeCardResponse, insuranceProfile, checkprofileComplete,
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
            var insuranceProfile = await _insuranceProfileRepository.GetByUserIdAsync(userId);
            if (insuranceProfile.SubscriptionStatus == false)
            {
                return new ResponseMessage { Message = "You have no active subscription", Status = false };
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
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task ProcessUserActiveStatusCancellation(int userId)
        {
            var insuranceProfile = await _insuranceProfileRepository.GetByUserIdAsync(userId);
            if (insuranceProfile.InsuranceService == "Hygeia")
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

                if(insuranceProfile.InsuranceService == "Hygeia")
                {
                    // Set enrollment and payment to processing. Check Model to see wat data is used for
                    var enrollment = new EnrollmentOnReactivation(insuranceProfile.UserId, insuranceProfile.Id, reactivationTime.Value, "Processing",
                       null,"Hygeia");
                    _enrollmentOnReactivation.Create(enrollment);
                    await _enrollmentOnReactivation.Save();

                    var paymentOnReactivation = new PaymentOnReactivation(insuranceProfile.UserId, insuranceProfile.Id, enrollment.Id, reactivationTime.Value
                        , "Processing", jobId, null, null,"Hygeia");
                    _paymentOnReactivation.Create(paymentOnReactivation);
                }
                else
                {
                    // Set enrollment and payment to processing. Check Model to see wat data is used for
                    var enrollment = new EnrollmentOnReactivation(insuranceProfile.UserId, insuranceProfile.Id, reactivationTime.Value, "Processing",
                       null, "Axamasard");
                    _enrollmentOnReactivation.Create(enrollment);
                    await _enrollmentOnReactivation.Save();

                    var paymentOnReactivation = new PaymentOnReactivation(insuranceProfile.UserId, insuranceProfile.Id, enrollment.Id, reactivationTime.Value
                        , "Processing", jobId, null, null, "Axamasard");
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

                if(insuranceProfile.InsuranceService == "Hygeia")
                {
                    // Send user details to hygeia
                    var registrationModel = _mapper.Map<RegistrationModel>(insuranceProfile);
                    var registration = await _insuranceSerivce.HygeiaRegisterUser(registrationModel);
                    if (registration.Status)
                    {
                        immediateEnrollment.Status = "Successful"; immediateEnrollment.Message = registration.Message; immediateEnrollment.InsuranceService = "Hygeia";
                        _enrollmentOnReactivation.Update(immediateEnrollment);
                    }
                    else
                    {
                        immediateEnrollment.Status = "Failed"; immediateEnrollment.Message = registration.Message; immediateEnrollment.InsuranceService = "Hygeia";
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
                        immediateEnrollment.Status = "Successful"; immediateEnrollment.Message = enrollment.Message;immediateEnrollment.InsuranceService = "Axamansard";
                        _enrollmentOnReactivation.Update(immediateEnrollment);
                    }
                    else
                    {
                        immediateEnrollment.Status = "Failed"; immediateEnrollment.Message = enrollment.Message; immediateEnrollment.InsuranceService = "Axamansard";
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

                if(insuranceProfile.InsuranceService == "Hygeia")
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

