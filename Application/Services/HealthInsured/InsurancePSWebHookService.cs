using Application.API_ResponseModel.Paystack;
using Application.HealthInsured_AxaMansard_Service.Insurance;
using Application.Helpers;
using Application.Interfaces;
using Application.Services.Card;
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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
        private readonly IEmailSender _emailSender;
        private readonly FamilyInsuranceService _familyInsurance;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IUniqueIdentifier _uniqueIdentifier;
        private readonly Card_SubscriptionService _cardService;
        private readonly HMOIntegrationService _hmoIntegrationService;
        private readonly Application.Helpers.Paystack _paystackOptions;

        private SubscriptionDuration SubscriptionAccessor { get; }

        public InsurancePSWebHookService(InsuranceService insuranceService, IRepositoryWrapper repoWrapper, ILogger<InsurancePSWebHookService> logger, PaystackService paystackService,
             IOptions<SubscriptionDuration> subscriptionAccessor, TokenizationService tokenizationService, CorporateInsuranceService corporateInsurance,IEmailSender emailSender,
             FamilyInsuranceService familyInsurance, IOptions<Application.Helpers.Paystack> paystackOptions, IHttpClientFactory httpClientFactory,
             IUniqueIdentifier uniqueIdentifier,Card_SubscriptionService cardService,HMOIntegrationService hmoIntegrationService)
        {
            _insuranceSerivce = insuranceService;
            _repoWrapper = repoWrapper;
            _logger = logger;
            _paystackService = paystackService;
            _tokenizationService = tokenizationService;
            _corporateInsurance = corporateInsurance;
            _emailSender = emailSender;
            _familyInsurance = familyInsurance;
            SubscriptionAccessor = subscriptionAccessor.Value;
            _httpClientFactory = httpClientFactory;
            _uniqueIdentifier = uniqueIdentifier;
            _cardService = cardService;
            _hmoIntegrationService = hmoIntegrationService;
            _paystackOptions = paystackOptions.Value;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task ForwardWebHookNotification(PaystackWebHookResponse webHookResponse)
        {
            var client = _httpClientFactory.CreateClient("HealthInsured_Fintech");
            var request = new HttpRequestMessage(new HttpMethod("POST"), _paystackOptions.Healthinsured_FintechWebhookURL)
            {
                Content = new StringContent(JsonConvert.SerializeObject(webHookResponse), Encoding.UTF8, "application/json")
            };
            await client.SendAsync(request);
            //if (!response.IsSuccessStatusCode)
            //{
            //    BackgroundJob.Enqueue(() => ForwardWebHookNotification(webHookResponse));
            //}
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task ProcessPaystackWebHook(string @event, string email, string reference, string authorization_code, string last4, string card_type, string amount)
        {
            if (@event == "charge.success")
            {
                _logger.LogInformation("Successful paystack webhook Charge");
                var status = "";

                var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByEmail(email);
                if (insuranceProfile != null)
                {
                    _logger.LogInformation("Process Webhook Payment for insurance");
                    var paymentReference = await _repoWrapper.PaymentReference.GetByReference(reference);
                    if (paymentReference != null)
                    {
                        _logger.LogInformation($"Payment reference [PaymentReference : {JsonConvert.SerializeObject(paymentReference)}]");
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
                        _logger.LogInformation("Process Webhook Payment for family");
                        var paymentReference = await _repoWrapper.PaymentReference.GetByReference(reference);
                        if (paymentReference != null)
                        {
                            _logger.LogInformation($"Payment reference [PaymentReference : {JsonConvert.SerializeObject(paymentReference)}]");
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
                            _logger.LogInformation("Process Webhook Payment for Company");
                            var paymentReference = await _repoWrapper.PaymentReference.GetByReference(reference);
                            if (paymentReference != null)
                            {
                                _logger.LogInformation($"Payment reference [PaymentReference : {JsonConvert.SerializeObject(paymentReference)}]");
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
            _logger.LogInformation($"Validate Webhook to Identify Profile Type ");
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
                _logger.LogInformation($"Process WebHook Succesful Individual Payment - FirstTime Payment [InsuranceUserProfile : {JsonConvert.SerializeObject(insuranceUserProfile)} " +
                    $"| Reference :{reference} | Amount : {amount}]");

                await ProcessWebHook_SuccessfulInsuranceIndividualPayment_FirstTimePayment(insuranceUserProfile);
                await _hmoIntegrationService.SendDetailsToInsuranceProvider(insuranceUserProfile);
            }
            if (decimal.Parse(amount) <= decimal.Parse("100"))
            {
                _logger.LogInformation($"Process WebHook Succesful Individual Payment - Refunding Payment ");
                BackgroundJob.Enqueue(() => _paystackService.RefundTestCardFunds(reference, (50 * 100).ToString()));
            }
        }

        private async Task ProcessWebHook_SuccessfulInsuranceIndividualPayment_FirstTimePayment(InsuranceUserProfile insuranceUserProfile)
        {
            
            insuranceUserProfile.EndActiveStatusDate = DateTime.Now.AddDays(SubscriptionAccessor.FreeTrialDayDuration);
            insuranceUserProfile.TransId = (insuranceUserProfile.InsuranceService == null | insuranceUserProfile.InsuranceService == InsuranceProvider.Axamansard.ToString()) ? _uniqueIdentifier.GetUniqueCode(10) : "";

            // Schedule debit email reminder for user 
            insuranceUserProfile.PendingEmailJobId = BackgroundJob.Schedule(() => _insuranceSerivce.SendEmailReminder(insuranceUserProfile.Email, insuranceUserProfile.Surname,"", null),
                   DateTime.Now.AddDays(SubscriptionAccessor.FreeTrialDayDuration).Subtract(new TimeSpan(3, 0, 0, 0)));

            //Schedule job to debit user every 28 days
            insuranceUserProfile.PendingJobId = _tokenizationService.ProcessScheduledPayment(insuranceUserProfile);

            var checkprofileComplete = await _repoWrapper.InsuranceCompletionProfile.GetCompletionStateByUserId(insuranceUserProfile.UserId.Value);
            checkprofileComplete.TokenizationCompleted = true;
            _repoWrapper.InsuranceCompletionProfile.Update(checkprofileComplete);

            await _emailSender.HealthInsuredSubscriptionMail(insuranceUserProfile.Email, "Active Free Trial", insuranceUserProfile.Surname, insuranceUserProfile.TransId,
               insuranceUserProfile.CareProviderName, insuranceUserProfile.PlanCode);

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
                _logger.LogInformation($"Process WebHook Succesful Corporate Payment [CompanyProfile : {JsonConvert.SerializeObject(companyProfile)} |" +
                $" Reference :{reference} | Amount :{amount}]");
                // if user is tokenizng or making payment for first time
                if (companyProfile.NextPaymentDate is null)
                {
                    companyProfile.NextPaymentDate = DateTime.Now.AddDays(SubscriptionAccessor.FreeTrialDayDuration);

                    //method to create insurance profiles for the beneficiaires.
                    var result = await _corporateInsurance.CreateInsuranceProfileForCompanyBeneficiaries(companyProfile.Id, null);

                    if (result.Status)
                    {
                        _logger.LogInformation($"Enqueue Onbording Corporate users");
                        //Background task to Enroll all users to HMO.
                       BackgroundJob.Enqueue(() => _hmoIntegrationService.OnboardCompanyUsersToHMO(companyProfile.UserId, null, companyProfile.NextPaymentDate.Value, companyProfile.InsuranceService));
                    }

                    //Background task to schedule debit at the end of next cycle
                    _logger.LogInformation($"Schedule Monthly Corporate Debit");
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
                _logger.LogInformation($"Process WebHook Succesful Corporate Payment - Refunding Payment ");
                BackgroundJob.Enqueue(() => _paystackService.RefundTestCardFunds(reference, (50 * 100).ToString()));
            }
            await Task.CompletedTask;
        }
        private async Task ProcessWebHook_SuccessfulFamilyPayment(FamilyProfile familyProfile, string reference, string amount)
        {
            _logger.LogInformation($"Process WebHook Succesful FamilyPayment [FamilyProfile : {JsonConvert.SerializeObject(familyProfile)} |" +
                $" Reference :{reference} | Amount :{amount}]");
            if (!familyProfile.TokenizationCompleted)
            {
                familyProfile.TokenizationCompleted = true;
                _repoWrapper.FamilyProfile.Update(familyProfile);
                await _repoWrapper.Save();
                await _cardService.FamilyMembersActivation(familyProfile);
                await _familyInsurance.FamilySubscription(familyProfile.Email, "Active Subscriptions", familyProfile.FullName);
            }
            if (decimal.Parse(amount) <= decimal.Parse("100"))
            {
                _logger.LogInformation($"Process WebHook Succesful FamilyPayment - Refunding Payment ");
                BackgroundJob.Enqueue(() => _paystackService.RefundTestCardFunds(reference, (50 * 100).ToString()));
            }
        }
    }
}
