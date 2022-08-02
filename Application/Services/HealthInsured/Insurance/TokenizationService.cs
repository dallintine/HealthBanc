
using Application.API_RequestModel.Paystack;
using Application.Helpers;
using Application.Interfaces;
using Application.Services.Paystack;
using Domain.Models;
using Domain.Models.Axa_Hygeia_Insurance;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;
using Domain.Models.Axa.Hygeia_Insurance;
using DataAccess;
using Microsoft.Extensions.Logging;
using Application.Services.HealthInsured;
namespace Application.Services.HealthInsured_AxaMansard.Insurance
{
    public class TokenizationService
    {
        private readonly PaystackService _paystackService;
        private readonly HMOIntegrationService _hmoIntegrationService;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<TokenizationService> _logger;
        private readonly IRepositoryWrapper _repoWrapper;

        private SubscriptionDuration SubscriptionAccessor { get; }


        public TokenizationService(PaystackService paystackService,HMOIntegrationService hmoIntegrationService,IOptions<SubscriptionDuration> subscriptionAccessor,
            IEmailSender emailSender,IRepositoryWrapper repoWrapper,ILogger<TokenizationService> logger)
        {
            _paystackService = paystackService;
            _hmoIntegrationService = hmoIntegrationService;
            _emailSender = emailSender;
            _logger = logger;
            SubscriptionAccessor = subscriptionAccessor.Value;
            _repoWrapper = repoWrapper;
        }

        /// <summary>
        /// Overloaded method to process individual scheduled payment.Create scheduled enrollment and scheduled payment data that is set to the processing stage.
        /// </summary>
        /// <param name="insuranceProfile"></param>
        /// <returns></returns>
        public string ProcessScheduledPayment(InsuranceUserProfile insuranceProfile)
        {
            _logger.LogInformation($"Processing Scheduled Payment\n");
            var executionDate = insuranceProfile.EndActiveStatusDate;

            var userId = (insuranceProfile.FamilyProfileId == null && insuranceProfile.InsurancePayeeId == null) ? insuranceProfile.UserId.Value : 0;
 
            var jobId = BackgroundJob.Schedule(() => SchedulePaymentLogic(userId,
                    insuranceProfile.Id, null), executionDate);

            return jobId;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task SchedulePaymentLogic(int userId, int axamansardUserId, PerformContext context)
        {
            _logger.LogInformation($"SchedulePayment Logic [UserId : {userId} |  insuranceProfileId : {axamansardUserId}] \n");
            var activeCard = new DebitCard();
            var family = new FamilyProfile();
            var payee = new InsuranceUserProfile();
            var jobId = context.BackgroundJob.Id;
            var chageAuthorizationModel = new ChargeAuthorization();

            var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByIdAsync(axamansardUserId);
            if (!(insuranceProfile.FamilyProfileId is null))
            {
                family = await _repoWrapper.FamilyProfile.GetFamilyByFamilyId(insuranceProfile.FamilyProfileId.Value);
                activeCard = family.Cards.FirstOrDefault(x => x.Status == (int)DebitCard_StatusValue.primary);
                chageAuthorizationModel.email = family.Email;
            }
            else if(!(insuranceProfile.InsurancePayeeId is null))
            {
                payee = await _repoWrapper.InsuranceProfile.GetByIdAsync(insuranceProfile.InsurancePayeeId.Value);
                activeCard = payee.Cards.FirstOrDefault(x => x.Status == (int)DebitCard_StatusValue.primary);
                chageAuthorizationModel.email = payee.Email;
            }
            else
            {
                activeCard = insuranceProfile.Cards.FirstOrDefault(x => x.Status == (int)DebitCard_StatusValue.primary);
                chageAuthorizationModel.email = insuranceProfile.Email;
            }


            chageAuthorizationModel.amount = (insuranceProfile.Premium * 100).ToString();
            chageAuthorizationModel.authorization_code = activeCard.Authorization_Code;

            var chargeAuthorization = await _paystackService.ChargeAuthorization(chageAuthorizationModel);
            if (chargeAuthorization.Status)
            {
                if (insuranceProfile.InsuranceService.ToLower() != InsuranceProvider.Hygeia.ToString().ToLower() || insuranceProfile.InsuranceService == null)
                {
                    if(insuranceProfile.AxamasardReferenceCode is null)
                    {
                        await _hmoIntegrationService.SendDetailsToInsuranceProvider(insuranceProfile);
                    }
                }

                if (insuranceProfile.FamilyProfileId != null)
                {
                    await Process_SuccessfulInsuranceIndividualPayment_ScheduledPayment(insuranceProfile, family,null, chargeAuthorization.Reference);
                }
                else if(insuranceProfile.InsurancePayeeId != null)
                {
                    await Process_SuccessfulInsuranceIndividualPayment_ScheduledPayment(insuranceProfile, null,payee, chargeAuthorization.Reference);
                }
                else
                {
                    await Process_SuccessfulInsuranceIndividualPayment_ScheduledPayment(insuranceProfile, null,null, chargeAuthorization.Reference);
                }
            }
            else
            {
                var channel = insuranceProfile.InsuranceService == InsuranceProvider.Hygeia.ToString() ? PaymentReference_ChannelValue.healthinsured_hygeia.ToString()
                    : PaymentReference_ChannelValue.healthinsured_axamansard.ToString();

                if (!(insuranceProfile.FamilyProfileId is null))
                {
                    //_insuranceSerivce.SendEmailOnFailedDebit(insuranceProfile.Email, insuranceProfile.Surname, insuranceProfile.Premium.ToString());
                    var paymentReference = new PaymentReference(channel, chargeAuthorization.Reference, null, null, family.Id, family.UserId
                    , insuranceProfile.Premium, PaymentReference_StatusValue.Failed.ToString());
                    _repoWrapper.PaymentReference.Create(paymentReference);
                    await Process_FailedInsuranceIndividualPayment_ScheduledPaument(insuranceProfile, family,null);
                }
                else if(!(insuranceProfile.InsurancePayeeId is null))
                {
                    //_insuranceSerivce.SendEmailOnFailedDebit(insuranceProfile.Email, insuranceProfile.Surname, insuranceProfile.Premium.ToString());
                    var paymentReference = new PaymentReference(channel, chargeAuthorization.Reference, payee.Id, null, null, payee.UserId.Value
                    , insuranceProfile.Premium, PaymentReference_StatusValue.Failed.ToString());
                    _repoWrapper.PaymentReference.Create(paymentReference);
                    await Process_FailedInsuranceIndividualPayment_ScheduledPaument(insuranceProfile, null,payee);
                }
                else
                {
                    //_insuranceSerivce.SendEmailOnFailedDebit(insuranceProfile.Email, insuranceProfile.Surname, insuranceProfile.Premium.ToString());
                    var paymentReference = new PaymentReference(channel, chargeAuthorization.Reference, insuranceProfile.Id, null, null, insuranceProfile.UserId.Value
                    , insuranceProfile.Premium, PaymentReference_StatusValue.Failed.ToString());
                    _repoWrapper.PaymentReference.Create(paymentReference);
                    await Process_FailedInsuranceIndividualPayment_ScheduledPaument(insuranceProfile, null,null);
                }
                _repoWrapper.InsuranceProfile.Update(insuranceProfile);
                await _repoWrapper.InsuranceProfile.Save();
                BackgroundJob.Delete(jobId);
            }
            await Task.CompletedTask;
        }

        public async Task Process_SuccessfulInsuranceIndividualPayment_ScheduledPayment(InsuranceUserProfile insuranceUserProfile, FamilyProfile family, InsuranceUserProfile payee, string reference)
        {
            _logger.LogInformation($"Process_SuccessfulInsuranceIndividualPayment_ScheduledPayment\n");
            insuranceUserProfile.StartActiveStatusDate =  DateTime.Now ;
            insuranceUserProfile.EndActiveStatusDate =  DateTime.Now.AddDays(SubscriptionAccessor.FreeTrialDayDuration);

            //Schedule job to debit user next 28 days
            insuranceUserProfile.PendingJobId = ProcessScheduledPayment(insuranceUserProfile);

            if (insuranceUserProfile.FamilyProfileId != null)
            {
                insuranceUserProfile.PendingEmailJobId = BackgroundJob.Schedule(() => SendFamilyPaymentReminder(family.Email, family.FullName
                        , insuranceUserProfile.Surname + " " + insuranceUserProfile.Othernames, null), insuranceUserProfile.EndActiveStatusDate.Subtract(new TimeSpan(3, 0, 0, 0)));
            }
            else if(insuranceUserProfile.InsurancePayeeId != null)
            {
                insuranceUserProfile.PendingEmailJobId = BackgroundJob.Schedule(() => SendEmailReminder(payee.Email, payee.Surname, "for your friend " + insuranceUserProfile.Surname, null)
                 , insuranceUserProfile.EndActiveStatusDate.Subtract(new TimeSpan(3, 0, 0, 0)));
            }
            else
            {
                insuranceUserProfile.PendingEmailJobId = BackgroundJob.Schedule(() => SendEmailReminder(insuranceUserProfile.Email, insuranceUserProfile.Surname,  "", null)
                , insuranceUserProfile.EndActiveStatusDate.Subtract(new TimeSpan(3, 0, 0, 0)));
            }
            // Schedule debit email reminder for user 
            insuranceUserProfile.SubscriptionStatus = true;
            insuranceUserProfile.ActiveStatus = true;
            insuranceUserProfile.FailedScheduledPaymentRetry = null;

            _repoWrapper.InsuranceProfile.Update(insuranceUserProfile);
            await _repoWrapper.Save();
        }

        private async Task Process_FailedInsuranceIndividualPayment_ScheduledPaument(InsuranceUserProfile insuranceUserProfile, FamilyProfile family,InsuranceUserProfile payee)
        {
            // If debit retries is less than 11 times Schedule debit for next four hours
            if (insuranceUserProfile.FailedScheduledPaymentRetry is null || insuranceUserProfile.FailedScheduledPaymentRetry < 11)
            {
                insuranceUserProfile.PendingEmailJobId = null;
                
                if(insuranceUserProfile.FailedScheduledPaymentRetry is null || insuranceUserProfile.FailedScheduledPaymentRetry < 4)
                {
                    Process_FailedInsuranceIndividualPaument_Email(insuranceUserProfile, family, payee, false);
                    insuranceUserProfile.PendingJobId = BackgroundJob.Schedule(() => SchedulePaymentLogic(0,insuranceUserProfile.Id, null), DateTime.Now.AddDays(SubscriptionAccessor.FailedIndividualPaymentRetryA));
                    insuranceUserProfile.FailedScheduledPaymentRetry = insuranceUserProfile.FailedScheduledPaymentRetry.HasValue ? insuranceUserProfile.FailedScheduledPaymentRetry += 1 : 1;
                }
                else if(insuranceUserProfile.FailedScheduledPaymentRetry > 3 && insuranceUserProfile.FailedScheduledPaymentRetry < 7)
                {
                    Process_FailedInsuranceIndividualPaument_Email(insuranceUserProfile, family, payee, false);
                    insuranceUserProfile.PendingJobId = BackgroundJob.Schedule(() => SchedulePaymentLogic(0, insuranceUserProfile.Id, null), DateTime.Now.AddDays(SubscriptionAccessor.FailedIndividualPaymentRetryB));
                    insuranceUserProfile.FailedScheduledPaymentRetry = insuranceUserProfile.FailedScheduledPaymentRetry.HasValue ? insuranceUserProfile.FailedScheduledPaymentRetry += 1 : 1;
                }
                else
                {
                    insuranceUserProfile.PendingEmailJobId = null; insuranceUserProfile.ActiveStatus = false; insuranceUserProfile.SubscriptionStatus = false;
                    insuranceUserProfile.PendingJobId = null;
                    insuranceUserProfile.FailedScheduledPaymentRetry = null;
                    Process_FailedInsuranceIndividualPaument_Email(insuranceUserProfile, family, payee, true);
                }
                _repoWrapper.InsuranceProfile.Update(insuranceUserProfile);
                await _repoWrapper.Save();               
            }
        }

        private void  Process_FailedInsuranceIndividualPaument_Email(InsuranceUserProfile insuranceProfile, FamilyProfile family, InsuranceUserProfile payee, bool deactivate)
        {
            if (!(insuranceProfile.FamilyProfileId is null))
            {
                if (deactivate)
                {
                    _emailSender.HealthInsuredDeactivationNotification(family.Email, "Deactivation Notification", family.FullName,
                        insuranceProfile.Premium.ToString(), $"{insuranceProfile.Othernames} {insuranceProfile.Surname}");
                }
                else
                {
                    _emailSender.HealthInsuredFailedDebitNotification(family.Email, "Failed Debit Notificaion", family.FullName, $"{insuranceProfile.Othernames} {insuranceProfile.Surname}", 
                        insuranceProfile.Premium.ToString());
                }
            }
            else if (!(insuranceProfile.InsurancePayeeId is null))
            {
                if (deactivate)
                {
                    _emailSender.HealthInsuredDeactivationNotification(payee.Email, "Failed Debit Notificaion", $"{insuranceProfile.Surname}",
                    insuranceProfile.Premium.ToString(), $"{insuranceProfile.Othernames} {insuranceProfile.Surname}");
                }
                else
                {
                    _emailSender.HealthInsuredFailedDebitNotification(payee.Email, "Failed Debit Notificaion", $"{insuranceProfile.Surname}", $"{insuranceProfile.Othernames} {insuranceProfile.Surname}",
                    insuranceProfile.Premium.ToString());
                }
            }
            else
            {
                if (deactivate)
                {
                    _emailSender.HealthInsuredDeactivationNotification(insuranceProfile.Email, "Failed Debit Notificaion", $"{insuranceProfile.Surname}", insuranceProfile.Premium.ToString(), "your");
                }
                else
                {
                    _emailSender.HealthInsuredFailedDebitNotification(insuranceProfile.Email, "Failed Debit Notificaion", $"{insuranceProfile.Surname}", "your", insuranceProfile.Premium.ToString());
                }
            }
        }

        /// <summary>
        /// Overloaded method to process corporate scheduled payment.Create scheduled enrollment and scheduled payment data that is set to the processing stage.
        /// </summary>
        /// <param name="insuranceProfile"></param>
        /// <returns></returns>
        public string ProcessScheduledPayment(CompanyProfile companyProfile)
        {
            var executionDate = companyProfile.NextPaymentDate.Value;

            var jobId = BackgroundJob.Schedule(() => SchedulePaymentLogic(companyProfile.UserId, null), executionDate);

            return jobId;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task SchedulePaymentLogic(int userId, PerformContext context)
        {
            var jobId = context.BackgroundJob.Id;
            var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyInsuranceUserProfilesByUserId(userId);
            var activeCard = companyProfile.Cards.FirstOrDefault(x => x.Status == (int)DebitCard_StatusValue.primary);
            Decimal premiumFee;

            var daysToLastPay = Math.Floor((DateTime.Now - companyProfile.NextPaymentDate.Value).TotalDays);

            if(daysToLastPay > 28)
            {
                premiumFee = companyProfile.InsuranceUserProfiles.Where(x => x.CompanySubscribedStatus == InsuranceProfile_CompanySubStatusValue.Active.ToString())
                    .Select(x => x.Premium).Sum();
            }
            else
            {
                premiumFee = companyProfile.InsuranceUserProfiles.Where(x => x.CompanySubscribedStatus == InsuranceProfile_CompanySubStatusValue.Active.ToString() ||
                x.CompanySubscribedStatus == InsuranceProfile_CompanySubStatusValue.Pending.ToString()).Select(x => x.Premium).Sum();
            }
            // set the next repayment date at first trial i.e when there is no failed scheduled payment attempt !!!!!
            if (companyProfile.FailedScheduledPaymentRetry is null)
            {
                companyProfile.NextPaymentDate = companyProfile.NextPaymentDate != null ? companyProfile.NextPaymentDate.Value.AddDays(SubscriptionAccessor.FreeTrialDayDuration) :
                    DateTime.Now.AddDays(SubscriptionAccessor.FreeTrialDayDuration);
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
                // if debit was successfully
                if (chargeAuthorization.Status)
                {

                    //Task to Enroll pending users to HMO
                    if(daysToLastPay <= 28)
                    {
                        await _hmoIntegrationService.OnboardCompanyUsersToHMO(companyProfile.UserId, InsuranceProfile_CompanySubStatusValue.Pending.ToString(), companyProfile.NextPaymentDate.Value,
                        companyProfile.InsuranceService);
                    }                    

                    //Background task to schedule debit at the end of next cycle
                    companyProfile.PendingJobId = ProcessScheduledPayment(companyProfile);

                    companyProfile.PendingEmailJobId = BackgroundJob.Schedule(() => SendEmailReminder(companyProfile.CompanyEmail, companyProfile.CompanyName, "", null),
                          companyProfile.NextPaymentDate.Value.Subtract(new TimeSpan(3, 0, 0, 0)));

                    companyProfile.FailedScheduledPaymentRetry = null;
                    companyProfile.TokenizationCompleted = true;

                    _repoWrapper.CompanyProfile.Update(companyProfile);

                    var companyBeneficiaires = companyProfile.InsuranceUserProfiles.Where(x => x.CompanySubscribedStatus == InsuranceProfile_CompanySubStatusValue.Active.ToString()).ToList();
                    foreach (var item in companyBeneficiaires)
                    {
                        item.StartActiveStatusDate = item.EndActiveStatusDate != default ? item.EndActiveStatusDate : DateTime.Now;
                        item.EndActiveStatusDate = item.EndActiveStatusDate != default ? item.EndActiveStatusDate.AddDays(SubscriptionAccessor.FreeTrialDayDuration)
                            : DateTime.Now.AddDays(SubscriptionAccessor.FreeTrialDayDuration);
                    }
                    _repoWrapper.InsuranceProfile.UpdateRange(companyBeneficiaires);

                    await _repoWrapper.Save();
                }
                else
                {
                    var channel = PaymentReference_ChannelValue.healthinsured_hygeia.ToString();

                    var paymentReference = new PaymentReference(channel, chargeAuthorization.Reference, null, companyProfile.Id, null, companyProfile.UserId
                        , premiumFee, PaymentReference_StatusValue.Failed.ToString());
                    _repoWrapper.PaymentReference.Create(paymentReference);

                    // If debit retries is less than 11 times Schedule debit for next four hours
                    if (companyProfile.FailedScheduledPaymentRetry is null || companyProfile.FailedScheduledPaymentRetry < 11)
                    {
                        companyProfile.PendingEmailJobId = null;

                        /// Schedule debit in the next 4 hours
                        companyProfile.PendingJobId = BackgroundJob.Schedule(() => SchedulePaymentLogic(companyProfile.UserId, null), DateTime.Now.AddDays(SubscriptionAccessor.FailedDebitRetrialDuration));

                        companyProfile.FailedScheduledPaymentRetry = companyProfile.FailedScheduledPaymentRetry.HasValue ? companyProfile.FailedScheduledPaymentRetry += 1 : 1;
                        _repoWrapper.CompanyProfile.Update(companyProfile);
                        await _repoWrapper.Save();

                        var leftProbationHours = (48 - (companyProfile.FailedScheduledPaymentRetry * 4));
                        var probationendDate = DateTime.Now.AddHours(Double.Parse(leftProbationHours.ToString()));

                        _emailSender.HealthInsuredFailedCompanyDebit(companyProfile.CompanyEmail, "Failed Transaction", companyProfile.CompanyName, premiumFee.ToString()
                            , probationendDate.ToLongDateString());
                    }
                    else
                    {
                        // Set debit to the next insurance cycle after payment probation hours (48 hr) or maximum retries is exceded & deactivate all beneficiaries
                        companyProfile.PendingEmailJobId = null;
                        companyProfile.PendingJobId = ProcessScheduledPayment(companyProfile);
                        companyProfile.FailedScheduledPaymentRetry = null;
                        _repoWrapper.CompanyProfile.Update(companyProfile);
                        await _repoWrapper.Save();

                        await DeactivateAllCompanybeneficiaries(companyProfile.UserId);

                        _emailSender.HealthInsuredCompanyDeactivation(companyProfile.CompanyEmail, "Deactivate Beneficiaries", companyProfile.CompanyName, premiumFee.ToString());
                    }
                }
            }
            else
            {
                var newJobId2 = ProcessScheduledPayment(companyProfile);
                companyProfile.PendingJobId = newJobId2;
                companyProfile.PendingEmailJobId = null;
                companyProfile.FailedScheduledPaymentRetry = null;
                _repoWrapper.CompanyProfile.Update(companyProfile);
                await _repoWrapper.Save();
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Deactivate all company insurance users
        /// </summary>
        /// <param name="companyUserId"></param>
        /// <returns></returns>
        public async Task DeactivateAllCompanybeneficiaries(int companyUserId)
        {
            var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyInsuranceUserProfilesByUserId(companyUserId);
            foreach (var item in companyProfile.InsuranceUserProfiles)
            {
                if (item.InsuranceService == InsuranceProvider.Hygeia.ToString())
                {
                    await _hmoIntegrationService.HygeiaDeactivateUser(item.TransId);
                }
                else
                {
                    if (item.AxamasardReferenceCode != null)
                    {
                        await _hmoIntegrationService.AxamansardDeactivateUser(item.AxamasardReferenceCode);
                    }
                }
                item.ActiveStatus = false;
                item.CompanySubscribedStatus = InsuranceProfile_CompanySubStatusValue.Inactive.ToString();
                item.SubscriptionStatus = false;
                _repoWrapper.InsuranceProfile.Update(item);
                await _repoWrapper.Save();
            }
            await Task.CompletedTask;
        }

        [AutomaticRetry(Attempts = 0)]
        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task ProcessUserActiveStatusCancellation(int userId)
        {
            var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByUserIdAsync(userId);
            if (insuranceProfile.InsuranceService.ToLower() == InsuranceProvider.Hygeia.ToString().ToLower())
            {
                await _hmoIntegrationService.HygeiaDeactivateUser(insuranceProfile.TransId);
            }
            else
            {
                if (insuranceProfile.AxamasardReferenceCode != null)
                {
                    await _hmoIntegrationService.AxamansardDeactivateUser(insuranceProfile.AxamasardReferenceCode);
                }
            }
            insuranceProfile.ActiveStatus = false;
            _repoWrapper.InsuranceProfile.Update(insuranceProfile);
            await _repoWrapper.Save();
            var insuranceProfile2 = await _repoWrapper.InsuranceProfile.GetByUserIdAsync(userId);
            if (insuranceProfile2.ActiveStatus is true)
            {
                insuranceProfile.ActiveStatus = false;
                _repoWrapper.InsuranceProfile.Update(insuranceProfile);
                await _repoWrapper.Save();
            }
            await Task.CompletedTask;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task ProcessUserActiveStatusCancellation(int insuranceProfileId,PerformContext performContext)
        {
            var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByIdAsync(insuranceProfileId);
            if (insuranceProfile.InsuranceService.ToLower() == InsuranceProvider.Hygeia.ToString().ToLower())
            {
                await _hmoIntegrationService.HygeiaDeactivateUser(insuranceProfile.TransId);
            }
            else
            {
                if (insuranceProfile.AxamasardReferenceCode != null)
                {
                    await _hmoIntegrationService.AxamansardDeactivateUser(insuranceProfile.AxamasardReferenceCode);
                }
            }
            insuranceProfile.ActiveStatus = false;
            _repoWrapper.InsuranceProfile.Update(insuranceProfile);
            await _repoWrapper.Save();
            var insuranceProfile2 = await _repoWrapper.InsuranceProfile.GetByIdAsync(insuranceProfileId);
            if (insuranceProfile2.ActiveStatus is true)
            {
                insuranceProfile.ActiveStatus = false;
                _repoWrapper.InsuranceProfile.Update(insuranceProfile);
                await _repoWrapper.Save();
            }
            await Task.CompletedTask;
        }       

        /// <summary>
        /// Function to send email reminder three days before subscription cysle ends.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="userName"></param>
        /// <param name="context"></param>
        public void SendEmailReminder(string email,string userName,string info, PerformContext context)
        {            
            _emailSender.SendHealthInsuredPaymentReminder(email,"Payment Reminder",userName,info);
        }

        public void SendEmailReminder(string email, string userName,  PerformContext context)
        {
            _emailSender.SendHealthInsuredPaymentReminder(email, "Payment Reminder", userName, "");
        }

        public void SendFamilyPaymentReminder(string email, string familyHead, string familyMember, PerformContext context)
        {
            _emailSender.SendHealthInsuredFamilyPaymentReminder(email, "HealthInsured Payment Reminder", familyHead, familyMember);
        }
    } 
}

