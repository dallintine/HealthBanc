using Application.HealthInsured_AxaMansard_Service.Insurance;
using Application.Helpers;
using Application.Services.HealthInsured.Insurance;
using Application.Services.HealthInsured_AxaMansard.Insurance;
using Application.Services.Paystack;
using DataAccess;
using Domain.Models;
using Domain.Models.Axa.Hygeia_Insurance;
using Domain.Models.Axa_Hygeia_Insurance;
using Domain.Models.ReportAndLogs;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.HealthInsured
{
    public class InsurancePSWebHookService
    {
        private readonly InsuranceService _insuranceSerivce;
        private readonly IRepositoryWrapper _repoWrapper;
        private readonly ILogger<InsurancePSWebHookService> _logger;
        private readonly PaystackService _paystackService;
        private readonly TokenizationService _tokenizationService;
        private readonly CorporateInsuranceService _corporateInsurance;
        private readonly FamilyInsuranceService _familyInsurance;

        private SubscriptionDuration SubscriptionAccessor { get; }

        public InsurancePSWebHookService(InsuranceService insuranceService, IRepositoryWrapper repoWrapper, ILogger<InsurancePSWebHookService> logger, PaystackService paystackService,
             IOptions<SubscriptionDuration> subscriptionAccessor, TokenizationService tokenizationService, CorporateInsuranceService corporateInsurance,
             FamilyInsuranceService familyInsurance)
        {
            _insuranceSerivce = insuranceService;
            _repoWrapper = repoWrapper;
            _logger = logger;
            _paystackService = paystackService;
            _tokenizationService = tokenizationService;
            _corporateInsurance = corporateInsurance;
            _familyInsurance = familyInsurance;
            SubscriptionAccessor = subscriptionAccessor.Value;
        }

        public async Task ProcessPaystackWebHook(string @event, string email, string reference, string authorization_code, string last4, string card_type, string amount)
        {
            _logger.LogInformation($"event {@event} | email : {email} | refeence : {reference} | authcode: {authorization_code} | last4 : {last4} | type : {card_type} |amount :{amount} ");
            _logger.LogInformation("Hit Pasytackwebhook.Successfully : " + DateTime.Now.ToLongDateString() + " : " + email + " : " + amount.ToString());

            if (@event == "charge.success")
            {
                _logger.LogCritical("Successful paystack webhook Charge");
                var status = "";

                var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByEmail(email);
                if (insuranceProfile != null)
                {
                    _logger.LogCritical("Process for insurance");
                    var paymentReference = await _repoWrapper.PaymentReference.GetByReference(reference);
                    if (paymentReference != null)
                    {
                        if (paymentReference.Status == PaymentReference_StatusValue.Send_Url.ToString())
                        {
                            status = PaymentReference_StatusValue.Send_Url.ToString();
                        }
                        paymentReference.Amount = decimal.Parse(amount);
                        paymentReference.Status = PaymentReference_StatusValue.Successful.ToString();
                        _repoWrapper.PaymentReference.Update(paymentReference);
                    }
                    else
                    {
                        // Create payment reference for the charge.
                        var channel = insuranceProfile.InsuranceService == InsuranceProvider.Hygeia.ToString() ? PaymentReference_ChannelValue.healthinsured_hygeia.ToString()
                            : PaymentReference_ChannelValue.healthinsured_axamansard.ToString();

                        var paymentReference2 = new PaymentReference(channel, reference, insuranceProfile.Id, null, null, insuranceProfile.UserId.Value
                        , decimal.Parse(amount), PaymentReference_StatusValue.Successful.ToString());
                        _repoWrapper.PaymentReference.Create(paymentReference2);
                    }
                    await _repoWrapper.Save();
                    await ValidateWebHookSuccesfulInsurancePayment(insuranceProfile, null, null, reference, authorization_code, last4, card_type, amount, status);
                }
                else 
                {
                    var familyProfile = await _repoWrapper.FamilyProfile.GetByEmail(email);
                    if (familyProfile != null)
                    {
                        _logger.LogCritical("Process for family");
                        var paymentReference = await _repoWrapper.PaymentReference.GetByReference(reference);
                        if (paymentReference != null)
                        {
                            if (paymentReference.Status == PaymentReference_StatusValue.Send_Url.ToString())
                            {
                                status = PaymentReference_StatusValue.Send_Url.ToString();
                            }
                            paymentReference.Amount = decimal.Parse(amount);
                            paymentReference.Status = PaymentReference_StatusValue.Successful.ToString();
                            _repoWrapper.PaymentReference.Update(paymentReference);
                        }
                        else
                        {
                            // Create payment reference for the charge.
                            var channel = familyProfile.InsuranceService == InsuranceProvider.Hygeia.ToString() ? PaymentReference_ChannelValue.healthinsured_hygeia.ToString()
                                : PaymentReference_ChannelValue.healthinsured_axamansard.ToString();

                            var paymentReference2 = new PaymentReference(channel, reference, null, null, familyProfile.Id, familyProfile.UserId
                            , decimal.Parse(amount), PaymentReference_StatusValue.Successful.ToString());
                            _repoWrapper.PaymentReference.Create(paymentReference2);
                        }
                        await _repoWrapper.Save();
                        await ValidateWebHookSuccesfulInsurancePayment(null, null, familyProfile, reference, authorization_code, last4, card_type, amount, status);
                    }
                    else
                    {
                        var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyProfileByEmail(email);
                        if (companyProfile != null)
                        {
                            _logger.LogCritical("Process for company");
                            var paymentReference = await _repoWrapper.PaymentReference.GetByReference(reference);
                            if (paymentReference != null)
                            {
                                if (paymentReference.Status == PaymentReference_StatusValue.Send_Url.ToString())
                                {
                                    status = PaymentReference_StatusValue.Send_Url.ToString();
                                }
                                paymentReference.Amount = decimal.Parse(amount);
                                paymentReference.Status = PaymentReference_StatusValue.Successful.ToString();
                                _repoWrapper.PaymentReference.Update(paymentReference);
                            }
                            else
                            {
                                // Create payment reference for the charge.
                                var channel = companyProfile.InsuranceService == InsuranceProvider.Hygeia.ToString() ? PaymentReference_ChannelValue.healthinsured_hygeia.ToString()
                                    : PaymentReference_ChannelValue.healthinsured_axamansard.ToString();

                                var paymentReference2 = new PaymentReference(channel, reference, null, companyProfile.Id, null, companyProfile.UserId
                                , decimal.Parse(amount), PaymentReference_StatusValue.Successful.ToString());
                                _repoWrapper.PaymentReference.Create(paymentReference2);
                            }
                            await _repoWrapper.Save();
                            await ValidateWebHookSuccesfulInsurancePayment(null, companyProfile, null, reference, authorization_code, last4, card_type, amount, status);
                        }
                    }                    
                }
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Method to make sure Users get value for their insurance payment
        /// </summary>
        /// <returns></returns>
        private async Task ValidateWebHookSuccesfulInsurancePayment(InsuranceUserProfile insuranceUserProfile, CompanyProfile companyProfile, FamilyProfile familyProfile,
            string reference, string authorization_code, string last4, string card_type, string amount, string status)
        {
            _logger.LogCritical($"Hit Card Status Processor {status}");
            if (!(insuranceUserProfile is null))
            {
                if (status == PaymentReference_StatusValue.Send_Url.ToString())
                {
                    int cardStatus = 0;
                    if (!(insuranceUserProfile.Cards.Any(x => x.Status == (int)DebitCard_StatusValue.primary)) || insuranceUserProfile.Cards.Count == 0)
                    {
                        cardStatus = (int)DebitCard_StatusValue.primary;
                    }

                    var debitCard = new DebitCard(insuranceUserProfile.UserId.Value, insuranceUserProfile.Id, null, null, cardStatus, last4, card_type
                    , reference, authorization_code);
                    _repoWrapper.Card.Create(debitCard);


                    var activityLog = new ActivityLog(insuranceUserProfile.Id, null, null, "Debit Card Added", ServiceNames.HealthInsured.ToString());
                    _repoWrapper.ActivityLog.Create(activityLog);
                    await _repoWrapper.Save();
                    await ProcessWebHook_SuccessfulInsuranceIndividualPayment(insuranceUserProfile, reference, amount);
                }
            }
            if (!(companyProfile is null))
            {
                if (status == PaymentReference_StatusValue.Send_Url.ToString())
                {
                    int cardStatus = 0;
                    if (!(companyProfile.Cards.Any(x => x.Status == (int)DebitCard_StatusValue.primary)) || companyProfile.Cards.Count == 0)
                    {
                        cardStatus = (int)DebitCard_StatusValue.primary;
                    }

                    var debitCard = new DebitCard(companyProfile.UserId, null, companyProfile.Id, null, cardStatus, last4, card_type
                    , reference, authorization_code);
                    _repoWrapper.Card.Create(debitCard);


                    var activityLog = new ActivityLog(null, companyProfile.Id, null, "Debit Card Added", ServiceNames.HealthInsured.ToString());
                    _repoWrapper.ActivityLog.Create(activityLog);
                    await _repoWrapper.Save();
                }
                await ProcessWebHook_SuccessfulCorporatePayment(companyProfile, reference, amount);
            }
            if (!(familyProfile is null))
            {
                if (status == PaymentReference_StatusValue.Send_Url.ToString())
                {
                    _logger.LogCritical("Hit Card Processor");
                    int cardStatus = 0;
                    if (!(familyProfile.Cards.Any(x => x.Status == (int)DebitCard_StatusValue.primary)) || familyProfile.Cards.Count == 0)
                    {
                        cardStatus = (int)DebitCard_StatusValue.primary;
                    }

                    var debitCard = new DebitCard(familyProfile.UserId, null, null, familyProfile.Id, cardStatus, last4, card_type
                    , reference, authorization_code);
                    _repoWrapper.Card.Create(debitCard);

                    var activityLog = new ActivityLog(null, null, familyProfile.Id, "Debit Card Added", ServiceNames.HealthInsured.ToString());
                    _repoWrapper.ActivityLog.Create(activityLog);
                    await _repoWrapper.Save();
                    await ProcessWebHook_SuccessfulFamilyPayment(familyProfile, reference, amount);
                }
            }
        }


        private async Task ProcessWebHook_SuccessfulInsuranceIndividualPayment(InsuranceUserProfile insuranceUserProfile, string reference, string amount)
        {
            // If user is making payment for the first time,
            if (insuranceUserProfile.SubscriptionStatus == null)
            {
                await ProcessWebHook_SuccessfulInsuranceIndividualPayment_FirstTimePayment(insuranceUserProfile);
                await _tokenizationService.SendDetailsToInsuranceProvider(insuranceUserProfile);
            }
            if (decimal.Parse(amount) <= decimal.Parse("100"))
            {
                BackgroundJob.Enqueue(() => _paystackService.RefundTestCardFunds(reference, (50 * 100).ToString()));
            }
        }

        private async Task ProcessWebHook_SuccessfulInsuranceIndividualPayment_FirstTimePayment(InsuranceUserProfile insuranceUserProfile)
        {
            
            insuranceUserProfile.EndActiveStatusDate = DateTime.Now.AddDays(SubscriptionAccessor.FreeTrialDayDuration);
            insuranceUserProfile.TransId = (insuranceUserProfile.InsuranceService == null | insuranceUserProfile.InsuranceService == InsuranceProvider.Axamansard.ToString()) ? _insuranceSerivce.GetUniqueCode() : "";

            // Schedule debit email reminder for user 
            insuranceUserProfile.PendingEmailJobId = BackgroundJob.Schedule(() => _insuranceSerivce.SendEmailReminder(insuranceUserProfile.Email, insuranceUserProfile.Surname,"", null),
                   DateTime.Now.AddDays(SubscriptionAccessor.FreeTrialDayDuration).Subtract(new TimeSpan(3, 0, 0, 0)));

            //Schedule job to debit user every 28 days
            insuranceUserProfile.PendingJobId = _tokenizationService.ProcessScheduledPayment(insuranceUserProfile);

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
        private async Task ProcessWebHook_SuccessfulCorporatePayment(CompanyProfile companyProfile, string reference, string amount)
        {
            if (decimal.Parse(amount) > 100)
            {
                // if user is tokenizng or making payment for first time
                if (companyProfile.NextPaymentDate is null)
                {
                    companyProfile.NextPaymentDate = DateTime.Now.AddDays(SubscriptionAccessor.FreeTrialDayDuration);

                    //method to create insurance profiles for the beneficiaires.
                    var result = await _corporateInsurance.CreateInsuranceProfileForCompanyBeneficiaries(companyProfile.Id, null);

                    if (result.Status)
                    {
                        //Background task to Enroll all users to HMO.
                        BackgroundJob.Enqueue(() => _corporateInsurance.OnboardCompanyUsersToHMO(companyProfile.UserId, null, companyProfile.NextPaymentDate.Value, companyProfile.InsuranceService));
                    }

                    //Background task to schedule debit at the end of next cycle
                    companyProfile.PendingJobId = _tokenizationService.ProcessScheduledPayment(companyProfile);

                    companyProfile.PendingEmailJobId = BackgroundJob.Schedule(() => _insuranceSerivce.SendEmailReminder(companyProfile.CompanyEmail, companyProfile.CompanyName,"", null),
                          DateTime.Now.AddDays(SubscriptionAccessor.FreeTrialDayDuration).Subtract(new TimeSpan(3, 0, 0, 0)));

                    companyProfile.TokenizationCompleted = true;
                    _repoWrapper.CompanyProfile.Update(companyProfile);
                    await _repoWrapper.Save();
                }
            }
            // Enqueue method to process refund 0f 50 naira test charge
            else
            {
                BackgroundJob.Enqueue(() => _paystackService.RefundTestCardFunds(reference, (50 * 100).ToString()));
            }
            await Task.CompletedTask;
        }

        private async Task ProcessWebHook_SuccessfulFamilyPayment(FamilyProfile familyProfile, string reference, string amount)
        {
            if (!familyProfile.TokenizationCompleted)
            {
                familyProfile.TokenizationCompleted = true;
                _repoWrapper.FamilyProfile.Update(familyProfile);
                await _repoWrapper.Save();
                await _tokenizationService.FamilyMembersActivation(familyProfile);
                _familyInsurance.FamilySubscription(familyProfile.Email, "Active Subscriptions", familyProfile.FullName);
            }
            if (decimal.Parse(amount) <= decimal.Parse("100"))
            {
                BackgroundJob.Enqueue(() => _paystackService.RefundTestCardFunds(reference, (50 * 100).ToString()));
            }
        }
    }
}
