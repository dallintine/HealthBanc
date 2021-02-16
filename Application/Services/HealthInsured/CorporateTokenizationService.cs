using Application.API_RequestModel.Paystack;
using Application.API_ResponseModel.Paystack;
using Application.DTO;
using Application.Helpers;
using Application.Services.Identity;
using Application.Services.Paystack;
using Application.ViewModels;
using AutoMapper;
using DataAccess.General.Implementation;
using DataAccess.HealthInsured.Interfaces;
using DataAccess.HealthInsured_AxaMansard.Interfaces;
using Domain.Models;
using Domain.Models.Axa.Hygeia_Insurance;
using Domain.Models.AxaMansard_Insurance;
using Hangfire;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.HealthInsured
{
    public class CorporateTokenizationService
    {
        private readonly ICompanyProfileRepository _companyProfileRepository;
        private readonly CardRepository _cardRepository;
        private readonly IMapper _mapper;
        private readonly TokenizationService _tokenizationService;
        private readonly IInsuranceProfileRepository _insuranceProfileRepository;
        private readonly IPaymentReferenceRepository _paymentReferenceRepository;
        private readonly PaystackService _paystackService;
        private readonly ICompanyInsuranceUserRepository _companyInsuranceUserRepository;
        private readonly IdentityService _identityService;
        private readonly IScheduledPaymentRepository _scheduledPayment;
        private SubscriptionDuration _subscriptionAccessor { get; }

        public CorporateTokenizationService(ICompanyProfileRepository companyProfileRepository, CardRepository cardRepository, IMapper mapper, TokenizationService tokenizationService,
            IInsuranceProfileRepository insuranceProfileRepository, IPaymentReferenceRepository paymentReferenceRepository, PaystackService paystackService
            , ICompanyInsuranceUserRepository companyInsuranceUserRepository, IdentityService identityService,IOptions<SubscriptionDuration> subscriptionAccessor,
            IScheduledPaymentRepository scheduledPayment)
        {
            _companyProfileRepository = companyProfileRepository;
            _cardRepository = cardRepository;
            _mapper = mapper;
            _tokenizationService = tokenizationService;
            _insuranceProfileRepository = insuranceProfileRepository;
            _paymentReferenceRepository = paymentReferenceRepository;
            _paystackService = paystackService;
            _companyInsuranceUserRepository = companyInsuranceUserRepository;
            _identityService = identityService;
            _subscriptionAccessor = subscriptionAccessor.Value;
            _scheduledPayment = scheduledPayment;
        }

        public async Task<ResponseMessage> TokenizeCard(ChargeCardViewModel chargeCard, List<string> emailAddress, int id, string ipAddress, string device)
        {
            var companyProfile = await _companyProfileRepository.GetCompanyProfileByUserId(id);
            if (companyProfile != null)
            {
                if (companyProfile.ProfileCompleted == true && companyProfile.EmailConfirmed)
                {
                    // remove empty space from the card.
                    var cardNumber = chargeCard.card.number.Replace(" ", "");

                    // Check if user has card dat matches last four card digit
                    var checkIfCardWasPreviouslyTokenized = await _cardRepository.CheckIfCardWasPreviouslyTokenized(id, cardNumber.Substring(cardNumber.Length - 4));
                    if (checkIfCardWasPreviouslyTokenized != null) return new ResponseMessage { Message = "This card was previously tokenized" };

                    var chargeCardRequest = _mapper.Map<API_RequestModel.Paystack.Card>(chargeCard.card);

                    var card = new ChargeCard();
                    card.card = chargeCardRequest; card.email = companyProfile.CompanyEmail;
                    card.reference = Guid.NewGuid().ToString(); card.pin = chargeCard.pin;

                    var insuranceUsers = await _insuranceProfileRepository.QueryableCompanyProfile(id);
                    var totalAmount = insuranceUsers.Where(x => x.CompanySubscribedStatus == "pending").Select(x => x.Premium).Sum();
                    card.amount = totalAmount.ToString();

                    // Create payment reference for the charge.
                    var paymentReference = new PaymentReference(card.reference, null,companyProfile.Id, id, totalAmount, "Pending");
                    _paymentReferenceRepository.Create(paymentReference);
                    await _paymentReferenceRepository.Save();

                    // Paystack service to charge user card
                    var chargeCardResponse = await _paystackService.ChargeCard(card, id);

                    // function to process response from paystack
                    return await ProcessPaystackChargeCardResponse(chargeCardResponse, companyProfile,paymentReference,emailAddress,card.reference, ipAddress, device);

                }
                return new ResponseMessage { Message = "User has not been profiled,kindly create your profile", Status = false };
            }
            return new ResponseMessage { Message = "User does not have a profile,kindly create your profile", Status = false };
        }

        public async Task<ResponseMessage> ProcessPaystackChargeCardResponse(TokenizationResponse chargeCardResponse, CompanyProfile companyProfile,
           PaymentReference paymentReference,List<string> emailAddress,string cardReference, string ipAddress, string device)
        {
            // if charge card was successfully
            if (chargeCardResponse.Status == true && chargeCardResponse.ResponseCode == 0)
            {
                //If card count is 0. it means there is no card available, so the card tokenised will
                //be the primary card so primary card status is set to 1 
                //else, card status is 0;
                var cardStatus = companyProfile.Cards.Count == 0 ? 1 : 0;

                var debitCard = new DebitCard(companyProfile.UserId, null, companyProfile.Id,cardStatus, chargeCardResponse.LastDigit, chargeCardResponse.Type
                    , cardReference, chargeCardResponse.AuthorizationCode);
                _cardRepository.Create(debitCard);

                //Background task to Enroll all users to hygeia.
                BackgroundJob.Enqueue(() => OnboardUsers(companyProfile.Id, emailAddress));

                //Background task to schedule debit at the end of next cycle
                var getScheduledPaymentJobId = await ProcessScheduledPayment(companyProfile);

                //// Schedule debit email reminder for user 
                //var emailReminderJobId = BackgroundJob.Schedule(() => SendEmailReminder(insuranceUserProfile.Email, insuranceUserProfile.Surname, null),
                //    DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration).Subtract(new TimeSpan(3, 0, 0, 0)));

                // Save scheduled debit job Id
                companyProfile.PendingJobId = getScheduledPaymentJobId;
                //Save scheduled email jobId
                //insuranceUserProfile.PendingEmailJobId = emailReminderJobId;

                companyProfile.NextPaymentDate = DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration);
                companyProfile.TokenizationCompleted = true;
                //companyProfile.PresentCyclePremiumFee = 

                //_companyProfileRepository.Update(insuranceUserProfile);

                //SendSuccesfulSubscriptionMail(insuranceUserProfile.Email, insuranceUserProfile.Surname, insuranceUserProfile.TransId, insuranceUserProfile.CareProviderName);

                ////Create Audit thats user subscrption changed 
                //var auditViewModel2 = new AuditLogViewModel(insuranceUserProfile.UserId, null, "Inactive subscription status", "Subscription Status Changed",
                //    "Active subscription status");
                //await _auditLogServices.UserCreateAuditLog(auditViewModel2, ipAddress, device);

                //await _insuranceProfileRepository.Save();

                //var auditViewModel = new AuditLogViewModel(insuranceUserProfile.UserId, null, null, "Debit Card Added", null);
                //await _auditLogServices.UserCreateAuditLog(auditViewModel, ipAddress, device);

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
                _paymentReferenceRepository.Update(paymentReference);
                await _paymentReferenceRepository.Save();
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
                _paymentReferenceRepository.Update(paymentReference);
                await _paymentReferenceRepository.Save();
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
            _paymentReferenceRepository.Update(paymentReference);
            await _paymentReferenceRepository.Save();
            return new ResponseMessage
            {
                Data = chargeCardResponse.Data,
                Message = chargeCardResponse.Message,
                Status = chargeCardResponse.Status,
                ResponseCode = chargeCardResponse.ResponseCode
            };
        }

        public async Task<string> ProcessScheduledPayment(CompanyProfile  companyProfile)
        {
            var executionDate = DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration);

            var jobId = BackgroundJob.Schedule(() => SchedulePaymentLogic(companyProfile.UserId, null), executionDate);
           
            var processingScheduledPayment = new ScheduledPayment(companyProfile.UserId, null, null, companyProfile.Id,executionDate, jobId, "Processing", null
                , null, "Hygeia");
            _scheduledPayment.Create(processingScheduledPayment);
            await _scheduledPayment.Save();
            return jobId;
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
                companyProfile.PendingJobId = newJobId; /*insuranceProfile.PendingEmailJobId = emailReminderJobId;*/
                _companyProfileRepository.Update(companyProfile);
                await _scheduledPayment.Save();
                await Task.CompletedTask;
            }
            //// Insufficient funds
            //else if (!chargeAuthorization.Status && chargeAuthorization.ResponseCode == 10)
            //{
            //    var paymentReference = new PaymentReference(chargeAuthorization.Reference, insuranceProfile.Id, null, insuranceProfile.UserId
            //        , insuranceProfile.Premium, "Failed");
            //    _paymentReference.Create(paymentReference);

            //    scheduledPaymentJob.Status = "Terminated"; scheduledPaymentJob.Message = chargeAuthorization.Message;
            //    scheduledPaymentJob.PaymentReference = chargeAuthorization.Reference;

            //    scheduledPaymentJob.ScheduledEnrollment.Status = "Terminated"; scheduledPaymentJob.ScheduledEnrollment.Message = "Terminated";
            //    scheduledPaymentJob.PaymentReference = chargeAuthorization.Reference;

            //    insuranceProfile.PendingJobId = null; insuranceProfile.SubscriptionStatus = false;
            //    insuranceProfile.ActiveStatus = false;
            //    _insuranceProfileRepository.Update(insuranceProfile);
            //    await _insuranceProfileRepository.Save();
            //    BackgroundJob.Delete(jobId);

            //    SendEmailOnFailedDebit(insuranceProfile.Email, insuranceProfile.Surname, insuranceProfile.Premium.ToString(), chargeAuthorization.Message);
            //    await Task.CompletedTask;
            //}
            //// Failed
            //else
            //{
            //    var paymentReference = new PaymentReference(chargeAuthorization.Reference, insuranceProfile.Id, null, insuranceProfile.UserId
            //       , insuranceProfile.Premium, "Failed");
            //    _paymentReference.Create(paymentReference);

            //    scheduledPaymentJob.Status = "Failed"; scheduledPaymentJob.Message = chargeAuthorization.Message;
            //    scheduledPaymentJob.ScheduledEnrollment.Status = "Failed"; scheduledPaymentJob.ScheduledEnrollment.Message = "Failed";
            //    scheduledPaymentJob.PaymentReference = chargeAuthorization.Reference;
            //    _scheduledPayment.Update(scheduledPaymentJob);

            //    insuranceProfile.PendingJobId = null; insuranceProfile.SubscriptionStatus = false;
            //    insuranceProfile.ActiveStatus = false;
            //    _insuranceProfileRepository.Update(insuranceProfile);
            //    await _insuranceProfileRepository.Save();
            //    BackgroundJob.Delete(jobId);

            //    SendEmailOnFailedDebit(insuranceProfile.Email, insuranceProfile.Surname, insuranceProfile.Premium.ToString(), chargeAuthorization.Message);

            //    await Task.CompletedTask;
            //}
            await Task.CompletedTask;
        }

        public async Task OnboardUsers(int companyId,List<String> emailAddress)
        {
            var companyInsuranceUsers = await _companyProfileRepository.GetCompanyInsuranceUsersByCompanyId(companyId);
            var pendingCompanyInsuranceUsers = companyInsuranceUsers.CompanyInsuranceUsers;
            foreach (var item in emailAddress)
            {
                var companyInsuranceUser = await _companyInsuranceUserRepository.GetByEmail(item);
                pendingCompanyInsuranceUsers.Remove(companyInsuranceUser);
            }
            var insuranceUserProfiles = new List<InsuranceUserProfile>();
            foreach (var item in pendingCompanyInsuranceUsers)
            {
                var user = _mapper.Map<ApplicationUser>(item);
                var createdUser = await _identityService.RegisterUserWithoutPassword(user);
                if (createdUser.Status)
                {
                    var userId = createdUser.Data.Id;
                    var insuranceUserProfile = _mapper.Map<InsuranceUserProfile>(item);
                    insuranceUserProfile.CompanyProfileId = companyId;
                    insuranceUserProfile.InsuranceService = "Hygeia";
                    insuranceUserProfile.Premium = Decimal.Parse("1000");
                    insuranceUserProfile.CompanySubscribedStatus = "active";
                    insuranceUserProfile.UserId = userId;
                    insuranceUserProfiles.Add(insuranceUserProfile);
                }
            }
            await _insuranceProfileRepository.InsertEntities(insuranceUserProfiles);
            foreach(var item in insuranceUserProfiles)
            {
                var result = await _tokenizationService.EnrollUserToHygeiaOnOnboarding(item);
                item.TransId = result.Message;
                _insuranceProfileRepository.Update(item);
            }
            await _insuranceProfileRepository.Save();
        }
    }
}


  