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

        private SubscriptionDuration SubscriptionAccessor { get; }

        public InsurancePSWebHookService(InsuranceService insuranceService,IRepositoryWrapper repoWrapper,ILogger<InsurancePSWebHookService> logger,PaystackService paystackService,
             IOptions<SubscriptionDuration> subscriptionAccessor,TokenizationService tokenizationService,CorporateInsuranceService corporateInsurance)
        {
            _insuranceSerivce = insuranceService;
            _repoWrapper = repoWrapper;
            _logger = logger;
            _paystackService = paystackService;
            _tokenizationService = tokenizationService;
            _corporateInsurance = corporateInsurance;
            SubscriptionAccessor = subscriptionAccessor.Value;
        }
        /// <summary>
        /// Process Paystack WebHook
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
            // Log it as criticall cause am not logging informations
            _logger.LogCritical("Hit Pasytackwebhook.Successfully : " + DateTime.Now.ToLongDateString() + " : " + email + " : " + amount.ToString());

            if (@event == "charge.success")
            {
                var status = "";

                var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByEmail(email);
                if (insuranceProfile != null)
                {
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

                        var paymentReference2 = new PaymentReference(channel, reference, insuranceProfile.Id, null,null, insuranceProfile.UserId.Value
                        , decimal.Parse(amount), PaymentReference_StatusValue.Successful.ToString());
                        _repoWrapper.PaymentReference.Create(paymentReference2);
                    }
                    await _repoWrapper.Save();
                    await ValidateWebHookSuccesfulInsurancePayment(insuranceProfile, null,null, reference, authorization_code, last4, card_type, amount, status);
                }
                else if(insuranceProfile is null)
                {
                    var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyProfileByEmail(email);
                    if (companyProfile != null)
                    {
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

                            var paymentReference2 = new PaymentReference(channel, reference, null, companyProfile.Id,null, companyProfile.UserId
                            , decimal.Parse(amount), PaymentReference_StatusValue.Successful.ToString());
                            _repoWrapper.PaymentReference.Create(paymentReference2);
                        }
                        await _repoWrapper.Save();
                        await ValidateWebHookSuccesfulInsurancePayment(null, companyProfile,null, reference, authorization_code, last4, card_type, amount, status);
                    }
                }
                else
                {
                    var familyProfile = await _repoWrapper.FamilyProfile.GetByEmail(email);
                    if(familyProfile != null)
                    {
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

                            var paymentReference2 = new PaymentReference(channel, reference, null,null, familyProfile.Id, familyProfile.UserId
                            , decimal.Parse(amount), PaymentReference_StatusValue.Successful.ToString());
                            _repoWrapper.PaymentReference.Create(paymentReference2);
                        }
                        await _repoWrapper.Save();
                        await ValidateWebHookSuccesfulInsurancePayment(null,null, familyProfile, reference, authorization_code, last4, card_type, amount, status);
                    }
                }
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Method to make sure Users get value for their insurance payment
        /// </summary>
        /// <returns></returns>
        private async Task ValidateWebHookSuccesfulInsurancePayment(InsuranceUserProfile insuranceUserProfile, CompanyProfile companyProfile,FamilyProfile familyProfile, string reference, string authorization_code, string last4, string card_type, string amount, string status)
        {
            if (!(insuranceUserProfile is null))
            {
                if (status == PaymentReference_StatusValue.Send_Url.ToString())
                {
                    int cardStatus = insuranceUserProfile.Cards.Count == 0 ? (int)DebitCard_StatusValue.primary : (int)DebitCard_StatusValue.secondary;
                    var debitCard = new DebitCard(insuranceUserProfile.UserId.Value, insuranceUserProfile.Id, null,null, cardStatus, last4, card_type
                    , reference, authorization_code);
                    _repoWrapper.Card.Create(debitCard);


                    var activityLog = new ActivityLog(insuranceUserProfile.Id, null,null, "Debit Card Added", ServiceNames.HealthInsured.ToString());
                    _repoWrapper.ActivityLog.Create(activityLog);
                    await _repoWrapper.Save();
                }
            }
            if (!(companyProfile is null))
            {
                if (status == PaymentReference_StatusValue.Send_Url.ToString())
                {
                    int cardStatus = companyProfile.Cards.Count == 0 ? (int)DebitCard_StatusValue.primary : (int)DebitCard_StatusValue.secondary;
                    var debitCard = new DebitCard(companyProfile.UserId, null, companyProfile.Id,null, cardStatus, last4, card_type
                    , reference, authorization_code);
                    _repoWrapper.Card.Create(debitCard);


                    var activityLog = new ActivityLog(null, companyProfile.Id,null, "Debit Card Added", ServiceNames.HealthInsured.ToString());
                    _repoWrapper.ActivityLog.Create(activityLog);
                    await _repoWrapper.Save();
                }
                await ProcessWebHook_SuccessfulCorporatePayment(companyProfile, reference, amount);
            }
            if (!(familyProfile is null))
            {
                if (status == PaymentReference_StatusValue.Send_Url.ToString())
                {
                    int cardStatus = familyProfile.Cards.Count == 0 ? (int)DebitCard_StatusValue.primary : (int)DebitCard_StatusValue.secondary;
                    var debitCard = new DebitCard(familyProfile.UserId, null, null ,familyProfile.Id, cardStatus, last4, card_type
                    , reference, authorization_code);
                    _repoWrapper.Card.Create(debitCard);


                    var activityLog = new ActivityLog(null,null, familyProfile.Id, "Debit Card Added", ServiceNames.HealthInsured.ToString());
                    _repoWrapper.ActivityLog.Create(activityLog);
                    await _repoWrapper.Save();
                }
            }
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
                        BackgroundJob.Enqueue(() => _corporateInsurance.OnboardCompanyUsersToHMO(companyProfile.UserId, null, companyProfile.NextPaymentDate.Value,companyProfile.InsuranceService));
                    }

                    //Background task to schedule debit at the end of next cycle
                    companyProfile.PendingJobId = _tokenizationService.ProcessScheduledPayment(companyProfile);

                    companyProfile.PendingEmailJobId = BackgroundJob.Schedule(() => _insuranceSerivce.SendEmailReminder(companyProfile.CompanyEmail, companyProfile.CompanyName, null),
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
    }
}
