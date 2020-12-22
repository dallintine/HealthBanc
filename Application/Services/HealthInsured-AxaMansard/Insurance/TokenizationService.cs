using Application.API_RequestModel.HealthInsured_AxaMansard;
using Application.API_RequestModel.Paystack;
using Application.API_ResponseModel.Paystack;
using Application.DTO;
using Application.HealthInsured_AxaMansard_Service.AuditAndReport.AuditLog;
using Application.HealthInsured_AxaMansard_Service.Insurance;
using Application.Helpers;
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
        private readonly IAxaEnrollmentOnOnboardingRepository _enrollmentOnOnboardingRepository;

        private SubscriptionDuration _subscriptionAccessor { get; }

        public TokenizationService(IAxaMansardUserProfileRepository mansardUserProfileRepository,IAxaMansardCompletionRepository completionRepository,
            ICardRepository cardRepository,IMapper mapper,PaystackService paystackService, AuditLogService auditLogServices, InsuranceService insuranceSerivce,
            IOptions<SubscriptionDuration> subscriptionAccessor, IScheduledPaymentRepository scheduledPayment, IScheduledAxaEnrollmentRepository scheduledAxaEnrollment
            ,IAxaEnrollmentReactivationRepository axaEnrollmentOnReactivation, IPaymentOnReactivationRepository paymentOnReactivation,
            IAxaEnrollmentOnOnboardingRepository enrollmentOnOnboardingRepository)
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
            _enrollmentOnOnboardingRepository = enrollmentOnOnboardingRepository;
            _subscriptionAccessor = subscriptionAccessor.Value;
        }

        /// <summary>
        /// This Method is Called for first time user tokenizing their card, and existing users adding new card, so method takes in logic
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
                    // if the user has an activate or deactivated subscription status, we do a test charge of 100 naria, we send amount in Kobo
                    // Since user is just adding a new card.
                    else
                    {
                        card.amount = (100 * 100).ToString();
                    }
                    
                    var chargeCardResponse = await _paystackService.ChargeCard(card, id);
                    return await ProcessPaystackChargeCardResponse(chargeCardResponse, userAxamansardProfile, checkprofileComplete,
                        card.reference, ipAddress, device);

                }
                return new ResponseMessage { Message = "User has not been profiled,kindly create your profile", Status = false };
            }
            return new ResponseMessage { Message = "User does not have a profile,kindly create your profile", Status = false };
        }

        private async Task<ResponseMessage> ProcessPaystackChargeCardResponse(TokenizationResponse chargeCardResponse, AxaMansardUserProfile userAxamansardProfile,
           AxaMansardCompletionProfile checkprofileComplete, string cardReference, string ipAddress, string device)
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
                    await EnrollUserToAxamansardOnOnboarding(userAxamansardProfile, null);

                    // Schedule Payment for user tokenizing card for the first time.
                    var getScheduledPaymentJobId = await ProcessScheduledPayment(userAxamansardProfile);

                    // Save scheduled job Id
                    userAxamansardProfile.PendingJobId = getScheduledPaymentJobId;

                    userAxamansardProfile.SubscriptionStatus = true;
                    userAxamansardProfile.ActiveStatus = true;
                    userAxamansardProfile.StartActiveStatusDate = DateTime.Now;
                    userAxamansardProfile.EndActiveStatusDate = DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration);

                    checkprofileComplete.TokenizationCompleted = true;
                    _completionRepository.Update(checkprofileComplete);

                    _mansardUserProfileRepository.Update(userAxamansardProfile);

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
                    Data = chargeCardResponse.Data,
                    Status = chargeCardResponse.Status,
                    ResponseCode = chargeCardResponse.ResponseCode,
                    Message = chargeCardResponse.Message
                };
            }
            // If charge card response request for OTP
            else if (chargeCardResponse.Status == true && chargeCardResponse.ResponseCode == 12)
            {
                return new ResponseMessage
                {
                    Data = chargeCardResponse.Data,
                    Message = chargeCardResponse.Message,
                    Status = chargeCardResponse.Status,
                    ResponseCode = chargeCardResponse.ResponseCode
                };
            }
            // If theres is an error
            return new ResponseMessage
            {
                Data = chargeCardResponse.Data,
                Message = chargeCardResponse.Message,
                Status = chargeCardResponse.Status,
                ResponseCode = chargeCardResponse.ResponseCode
            };
        }

        public async Task EnrollUserToAxamansardOnOnboarding(AxaMansardUserProfile userAxamansardProfile, PerformContext context)
        {
            // Send user details to axamansard
            var enrollmentModel = _mapper.Map<EnrollmentModel>(userAxamansardProfile);
            var enrollment = await _insuranceSerivce.EnrollUser(enrollmentModel);
            if (!enrollment.Status)
            {
                var jobId = context.BackgroundJob.Id;
                var axaEnrollmentOnOnboarding = new AxaEnrollmentOnOnboarding(userAxamansardProfile.UserId, userAxamansardProfile.Id, jobId, "Failed", enrollment.Message);
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
                executionDate, jobId, "Processing",null);
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
                scheduledPaymentJob.Status = "Terminated"; scheduledPaymentJob.Message = chargeAuthorization.Message;

                scheduledPaymentJob.ScheduledAxaEnrollment.Status = "Terminated"; scheduledPaymentJob.ScheduledAxaEnrollment.Message = "Terminated";

                axaMansardProfile.PendingJobId = null; axaMansardProfile.SubscriptionStatus = false;
                axaMansardProfile.ActiveStatus = false;
                _mansardUserProfileRepository.Update(axaMansardProfile);
                await _mansardUserProfileRepository.Save();
                BackgroundJob.Delete(jobId);
                await Task.CompletedTask;
            }
            else
            {
                scheduledPaymentJob.Status = "Failed"; scheduledPaymentJob.Message = chargeAuthorization.Message;
                scheduledPaymentJob.ScheduledAxaEnrollment.Status = "Failed"; scheduledPaymentJob.ScheduledAxaEnrollment.Message = "Failed";
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
            if (checkprofileComplete != null)
            {
                if (checkprofileComplete.ProfileCompleted == true)
                {
                    var chargeCardResponse = await _paystackService.SendOtp(otpViewModel.otp, otpViewModel.reference, userAxamansardProfile.PhoneNumber
                        , userAxamansardProfile.DateOfBirth);
                    return await ProcessPaystackChargeCardResponse(chargeCardResponse, userAxamansardProfile, checkprofileComplete,
                        otpViewModel.reference,ipAddress,device);
                }
                return new ResponseMessage { Message = "User has not been profiled,kindly create your profile", Status = false };
            }
            return new ResponseMessage { Message = "User does not have a profile,kindly create your profile", Status = false };
        }

        public async Task<ResponseMessage> CancelSubscription(int userId,string reason)
        {
            var axamansardprofile = await _mansardUserProfileRepository.GetByUserIdAsync(userId);
            if (axamansardprofile.SubscriptionStatus == false)
            {
                return new ResponseMessage { Message = "You have no active subscription", Status = false };
            }

            var scheduledJobId = axamansardprofile.PendingJobId;

            var scheduledPayment = await _scheduledPayment.GetScheduledPaymentByJobId(scheduledJobId);
            if(scheduledPayment != null)
            {
                BackgroundJob.Delete(scheduledJobId);
                scheduledPayment.Status = "Cancelled"; scheduledPayment.Message = "Cancelled";
                scheduledPayment.ScheduledAxaEnrollment.Status = "Cancelled"; scheduledPayment.ScheduledAxaEnrollment.Message = "Cancelled";
                _scheduledPayment.Update(scheduledPayment);
            }
            else
            {
                var scheduledReactivatedPayment = await _paymentOnReactivation.GetScheduledPaymentByJobId(scheduledJobId);
                if(scheduledReactivatedPayment != null)
                {
                    BackgroundJob.Delete(scheduledJobId);
                    scheduledReactivatedPayment.Status = "Cancelled"; scheduledReactivatedPayment.Message = "Cancelled";
                    scheduledReactivatedPayment.AxaEnrollmentOnReactivation.Status = "Cancelled";
                    scheduledReactivatedPayment.AxaEnrollmentOnReactivation.Message = "Cancelled";
                    _paymentOnReactivation.Update(scheduledReactivatedPayment);
                }
            } 
           

            var daysToCancelUserActivityStatus = axamansardprofile.EndActiveStatusDate;
            if(daysToCancelUserActivityStatus == DateTime.Now.Date)
            {
                axamansardprofile.ActiveStatus = false;
                axamansardprofile.PendingJobId = null;
            }
            else
            {
                var jobId = BackgroundJob.Schedule(() => ProcessUserActiveStatusCancellation(axamansardprofile.UserId), daysToCancelUserActivityStatus);
                axamansardprofile.PendingJobId = jobId;
            }

            axamansardprofile.SubscriptionStatus = false;
            _mansardUserProfileRepository.Update(axamansardprofile);
            await _scheduledPayment.Save();
            var profileDTO = _mapper.Map<AxaMansardUserDTO>(axamansardprofile);
            return new ResponseMessage { Data = profileDTO, Message = "Subscription was canceled successfully.However you remain active till " +
                "your insurance cycle ends.", Status = true };
        }

        public async Task ProcessUserActiveStatusCancellation(int userId)
        {
            var axamansardprofile = await _mansardUserProfileRepository.GetByUserIdAsync(userId);
            axamansardprofile.ActiveStatus = false;
            _mansardUserProfileRepository.Update(axamansardprofile);
            await _mansardUserProfileRepository.Save();
            await Task.CompletedTask;
        }

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

        private async Task<ResponseMessage> ProcessReactivationFlow(AxaMansardUserProfile userAxamansardProfile, string authorization_Code,DateTime? reactivationTime)
        {
            if (reactivationTime is null)
            {   
                return await ProcessImmediateReactivationPayment(userAxamansardProfile, authorization_Code);
            }
            else
            {
                //Delete ProcessUserActiveStatusCancellation if it exist; Cause user may wanna reactivate before cycle ends.
                BackgroundJob.Delete(userAxamansardProfile.PendingJobId);
                var jobId = BackgroundJob.Schedule(() => ProcessScheduledReactivationPayment(userAxamansardProfile.UserId, authorization_Code), reactivationTime.Value);

                var axaEnrollment = new AxaEnrollmentOnReactivation(userAxamansardProfile.UserId, userAxamansardProfile.Id, reactivationTime.Value, "Processing",
                   null);
                _axaEnrollmentOnReactivation.Create(axaEnrollment);
                await _axaEnrollmentOnReactivation.Save();

                var paymentOnReactivation = new PaymentOnReactivation(userAxamansardProfile.UserId, userAxamansardProfile.Id, axaEnrollment.Id, reactivationTime.Value
                    , "Processing", jobId, null);
                _paymentOnReactivation.Create(paymentOnReactivation);

                userAxamansardProfile.SubscriptionStatus = true;
                userAxamansardProfile.PendingJobId = jobId;
                await _axaEnrollmentOnReactivation.Save();
                return new ResponseMessage {Status = true,Message= "Reactivation was successful.You will be debited a the end of your " +
                    "active cycle" };
            }            
        }

        public async Task<ResponseMessage> ProcessImmediateReactivationPayment(AxaMansardUserProfile userAxamansardProfile, string authorization_Code)
        {
            var chageAuthorizationModel = new ChargeAuthorization()
            {
                email = userAxamansardProfile.Email,
                amount = (userAxamansardProfile.Premium * 100).ToString(),
                authorization_code = authorization_Code
            };

            var chargeAuthorization = await _paystackService.ChargeAuthorization(chageAuthorizationModel);
            if (chargeAuthorization.Status)
            {
                var immediateAxaEnrollment = new AxaEnrollmentOnReactivation(userAxamansardProfile.UserId, userAxamansardProfile.Id, DateTime.Now, "Processing", null);
                _axaEnrollmentOnReactivation.Create(immediateAxaEnrollment);
                await _axaEnrollmentOnReactivation.Save();

                var enrollmentModel = _mapper.Map<EnrollmentModel>(userAxamansardProfile);
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
                 DateTime.Now, "Successful",null, null);
                _paymentOnReactivation.Create(immediatePaymentOnReactivation);
               
                var getScheduledPaymentJobId = await ProcessScheduledPayment(userAxamansardProfile);
                userAxamansardProfile.SubscriptionStatus = true;userAxamansardProfile.PendingJobId = getScheduledPaymentJobId;
                userAxamansardProfile.ActiveStatus = true; userAxamansardProfile.StartActiveStatusDate = DateTime.Now;
                userAxamansardProfile.EndActiveStatusDate = DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration);
                _mansardUserProfileRepository.Update(userAxamansardProfile);

                await _mansardUserProfileRepository.Save();
                return new ResponseMessage { Message = "Reactivation was successful." , Status = true, ResponseCode = chargeAuthorization.ResponseCode };
            }
            return new ResponseMessage { Message = chargeAuthorization.Message, Status =false, ResponseCode = chargeAuthorization.ResponseCode };
        }

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

            var chargeAuthorization = await _paystackService.ChargeAuthorization(chageAuthorizationModel);
            var paymentOnReactivation = await _paymentOnReactivation.GetScheduledPaymentByStatus("Processing");
            if (chargeAuthorization.Status)
            {                
                paymentOnReactivation.Status = "Successful"; paymentOnReactivation.Message = chargeAuthorization.Message;
                _paymentOnReactivation.Update(paymentOnReactivation);

                var enrollmentModel = _mapper.Map<EnrollmentModel>(userAxamansardProfile);
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
                var getScheduledPaymentJobId = await ProcessScheduledPayment(userAxamansardProfile);

                // Save scheduled job Id
                userAxamansardProfile.PendingJobId = getScheduledPaymentJobId; userAxamansardProfile.StartActiveStatusDate = DateTime.Now;
                userAxamansardProfile.SubscriptionStatus = true; userAxamansardProfile.ActiveStatus = true;
                userAxamansardProfile.EndActiveStatusDate = DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration);
                _mansardUserProfileRepository.Update(userAxamansardProfile);
            }
            else if(!chargeAuthorization.Status && chargeAuthorization.ResponseCode == 10)
            {
                paymentOnReactivation.Status = "Successful"; paymentOnReactivation.Message = chargeAuthorization.Message;
                paymentOnReactivation.AxaEnrollmentOnReactivation.Status = "Terminated"; paymentOnReactivation.AxaEnrollmentOnReactivation.Message = "Terminated";
                _paymentOnReactivation.Update(paymentOnReactivation);

                userAxamansardProfile.PendingJobId = null; userAxamansardProfile.SubscriptionStatus = false;
                userAxamansardProfile.ActiveStatus = false;
                _mansardUserProfileRepository.Update(userAxamansardProfile);
            }
            else
            {
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
    } 
}




//BackgroundJob.Schedule(() => _tokenizationService.SendEmailReminder(use, _templateId.HealthInsured_PaymentReminder, null),
//    DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration).Subtract(new TimeSpan(3, 0, 0, 0)));