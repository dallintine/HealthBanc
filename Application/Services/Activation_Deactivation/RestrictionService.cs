using Application.API_RequestModel.Paystack;
using Application.DTO;
using Application.HealthInsured_AxaMansard_Service.Insurance;
using Application.Helpers;
using Application.Interfaces;
using Application.Services.HealthInsured;
using Application.Services.HealthInsured.Insurance;
using Application.Services.HealthInsured_AxaMansard.Insurance;
using Application.Services.Paystack;
using Application.Services.Wallet;
using Application.ViewModels.HealthInsured;
using AutoMapper;
using DataAccess;
using Domain.Enums;
using Domain.Models;
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

namespace Application.Services.Activation_Deactivation
{
    public class RestrictionService
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly ILogger<RestrictionService> _logger;
        private readonly IMapper _mapper;
        private readonly IUniqueIdentifier _uniqueIdentifier;
        private readonly TokenizationService _tokenizationService;
        private readonly PaystackService _paystackService;
        private readonly FamilyInsuranceService _familyInsurance;
        private readonly HMOIntegrationService _hmoIntegrationService;
        private readonly WalletPaymentService _walletPaymentService;

        private SubscriptionDuration SubscriptionAccessor { get; }

        public RestrictionService(IRepositoryWrapper repositoryWrapper, ILogger<RestrictionService> logger, IMapper mapper, IOptions<SubscriptionDuration> subscriptionAccessor
            , IUniqueIdentifier uniqueIdentifier, TokenizationService tokenizationService, PaystackService paystackService, FamilyInsuranceService familyInsurance,
            HMOIntegrationService hmoIntegrationService, WalletPaymentService walletPaymentService)
        {
            _repositoryWrapper = repositoryWrapper;
            _logger = logger;
            _mapper = mapper;
            _uniqueIdentifier = uniqueIdentifier;
            _tokenizationService = tokenizationService;
            _paystackService = paystackService;
            _familyInsurance = familyInsurance;
            _hmoIntegrationService = hmoIntegrationService;
            _walletPaymentService = walletPaymentService;
            SubscriptionAccessor = subscriptionAccessor.Value;
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

        public async Task<ResponseMessage> DeactivateFamilyMember(InsuranceUserProfile insuranceProfile)
        {
            return await ProcessCancelSubscription(insuranceProfile);
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

        /// <summary>
        /// Reactivate user Subscription with user primary card.
        /// If user Subscription status is false and cycle is active. Background job is scheduled to reactivate the user when the cysle ends.
        /// Else user subscription is reactivated immediately
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> Reactivate(int userId)
        {
            var insuranceProfile = await _repositoryWrapper.InsuranceProfile.GetByUserIdAsync(userId);
            string paymentMethod = insuranceProfile.PaymentMethod;
            string authorizationCode = null;
            if (insuranceProfile.SubscriptionStatus == true)
            {
                return new ResponseMessage { Message = "Subscription is currently active", Status = false };
            }

            if(paymentMethod is null || paymentMethod == PaymentMethod.Card.ToString())
            {
                var primaryCard = insuranceProfile.Cards.FirstOrDefault(x => x.Status == (int)DebitCard_StatusValue.primary);
                if (primaryCard == null)
                {
                    return new ResponseMessage { Message = "Kindly add a primary card, then start the reactivation process" };
                }
                authorizationCode = primaryCard.Authorization_Code;
            }
            else
            {
                if(insuranceProfile.UserWallet is null)
                {
                    return new ResponseMessage { Message = "Kindly set up wallet, then start the reactivation process" };
                }
            }
            
            // If  user is not in an active cycle
            if (insuranceProfile.ActiveStatus == false)
            {
                return await ProcessImmediateReactivationPayment(insuranceProfile, insuranceProfile.UserId.Value, authorizationCode, paymentMethod);
            }
            // If user is in an active cycle
            else
            {
                // We pass  the time 
                return await ProcessScheduledReactivationFlow(insuranceProfile, insuranceProfile.EndActiveStatusDate);
            }
        }

        /// <summary>
        /// Activate a new family member with the family primary debit card
        /// </summary>
        /// <param name="familyUserId"></param>
        /// <param name="insuranceProfileId"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> ActivateFamilyMember(int familyUserId, int insuranceProfileId)
        {
            var familyProfile = await _repositoryWrapper.FamilyProfile.GetExtendedFamilyDetails(familyUserId);
            var insuranceProfile = familyProfile.InsuranceUserProfiles.Where(x => x.Id == insuranceProfileId).FirstOrDefault();
            string paymentMethod = familyProfile?.PaymentMethod;
            string authorizationCode = null;
            if (insuranceProfile != null)
            {
                if (insuranceProfile.SubscriptionStatus == true)
                {
                    return new ResponseMessage { Message = "Subscription is currently active", Status = false };
                }
                if (paymentMethod is null || paymentMethod == PaymentMethod.Card.ToString())
                {
                    var primaryCard = familyProfile.Cards.FirstOrDefault(x => x.Status == (int)DebitCard_StatusValue.primary);
                    if (primaryCard == null)
                    {
                        return new ResponseMessage { Message = "Kindly add a primary card, then start the activation process" };
                    }
                    authorizationCode = primaryCard.Authorization_Code;
                }
                else
                {
                    if (insuranceProfile.UserWallet is null)
                    {
                        return new ResponseMessage { Message = "Kindly set up wallet, then start the reactivation process" };
                    }
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
                    var response = await ProcessImmediateReactivationPayment(insuranceProfile, familyProfile.UserId, authorizationCode , paymentMethod);
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

        public async Task<ResponseMessage> ActivateReferee(int userId, int refereeInsuranceId)
        {
            var payeeInsuranceProfile = await _repositoryWrapper.InsuranceProfile.GetByUserIdAsync(userId);
            var refereedInsuranceProfile = await _repositoryWrapper.InsuranceProfile.GetByIdAsync(refereeInsuranceId);
            string paymentMethod = payeeInsuranceProfile?.PaymentMethod;
            string authorizationCode = null;

            if (refereedInsuranceProfile != null)
            {
                if (refereedInsuranceProfile.SubscriptionStatus == true)
                {
                    return new ResponseMessage { Message = "Subscription is currently active", Status = false };
                }

                if (paymentMethod is null || paymentMethod == PaymentMethod.Card.ToString())
                {
                    var primaryCard = payeeInsuranceProfile?.Cards.FirstOrDefault(x => x.Status == (int)DebitCard_StatusValue.primary);
                    if (primaryCard == null)
                    {
                        return new ResponseMessage { Message = "Kindly add a primary card, then start the activation process" };
                    }
                    authorizationCode = primaryCard.Authorization_Code;
                }
                else
                {
                    if (payeeInsuranceProfile?.UserWallet is null)
                    {
                        return new ResponseMessage { Message = "Kindly set up wallet, then start the reactivation process" };
                    }
                }

               
                // If  user is not in an active cycle
                if (refereedInsuranceProfile.ActiveStatus == false)
                {
                    var response = await ProcessImmediateReactivationPayment(refereedInsuranceProfile, userId, authorizationCode, paymentMethod);
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
            return new ResponseMessage { Message = "Insurance profile does not exist under your profile!" };
        }

        /// <summary>
        /// Func to process immediate user reactivation
        /// </summary>
        /// <param name="userAxamansardProfile"></param>
        /// <param name="authorization_Code"></param>
        /// <returns></returns>
        private async Task<ResponseMessage> ProcessImmediateReactivationPayment(InsuranceUserProfile insuranceProfile, int userId, string authorization_Code, string paymentmethod)
        {
            _logger.LogInformation($"ProcessImmediateReactivationPayment [UserId : {userId} | InsuranceProfile : {JsonConvert.SerializeObject(insuranceProfile)}]\n");
            FamilyProfile family = null;
            InsuranceUserProfile payee = null;
            var channel = insuranceProfile.InsuranceService == InsuranceProvider.Hygeia.ToString() ? PaymentReference_ChannelValue.healthinsured_hygeia.ToString()
                : PaymentReference_ChannelValue.healthinsured_axamansard.ToString();
            bool paymentStatus = false;
            string reference = null;

            if (paymentmethod == PaymentMethod.Wallet.ToString())
            {
                string walletPhoneNumber = null;
                if(insuranceProfile.FamilyProfileId != null)
                {
                    var familyprofile = await _repositoryWrapper.FamilyProfile.GetWalletByUserId(userId);
                    walletPhoneNumber = familyprofile.UserWallet.Mobile;
                }
                else
                {
                    var insuranceprofile = await _repositoryWrapper.InsuranceProfile.GetWalletByUserIdAsync(userId);
                    walletPhoneNumber = insuranceprofile.UserWallet.Mobile;
                }               
                var walletTransfer = await _walletPaymentService.WalletToSterling(userId, insuranceProfile.Premium, walletPhoneNumber, channel);
                if (walletTransfer.Status) paymentStatus = true;
            }
            else
            {
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
                if (chargeAuthorization.Status) paymentStatus = true;
                reference = chargeAuthorization.Reference;
            }   
            
            if (paymentStatus)
            {
                await Process_SuccessfulInsuranceIndividualPayment_ImmediateReactivationPayment(insuranceProfile, family, payee);
                await _hmoIntegrationService.SendDetailsToInsuranceProvider(insuranceProfile);
                return new ResponseMessage { Message = "Reactivation was successful.", Status = true, ResponseCode = 00 };
            }
            // Setfailed payment reference for transacation;            

            if (insuranceProfile.FamilyProfileId is null)
            {
                var paymentReference = new PaymentReference(channel, reference, insuranceProfile.Id, null, null, userId
                   , insuranceProfile.Premium, PaymentReference_StatusValue.Failed.ToString());
                _repositoryWrapper.PaymentReference.Create(paymentReference);
            }
            else
            {
                var paymentReference = new PaymentReference(channel, reference, null, null, insuranceProfile.FamilyProfileId, userId
                  , insuranceProfile.Premium, PaymentReference_StatusValue.Failed.ToString());
                _repositoryWrapper.PaymentReference.Create(paymentReference);
            }

            await _repositoryWrapper.Save();

            return new ResponseMessage { Message = reference, Status = false, ResponseCode = 12 };
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

            // Schedule debit email reminder for individual user 
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

       
    }
}
