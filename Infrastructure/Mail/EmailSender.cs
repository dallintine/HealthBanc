using Application.API_RequestModel;
using Application.DTO;
using Application.Helpers;
using Application.Helpers.ThirdPartyAPI;
using Application.Interfaces;
using Application.ViewModels;
using Infrastructure.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Mail
{
    public class EmailSender : IEmailSender
    {
        private readonly ILogger<EmailSender> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string BaseUrl;

        public Application.Helpers.Environment EnvironmentAccessor { get; }
        private EmailAuth EmailAccessor { get; }
        public EmailSender(IOptions<Application.Helpers.Environment>environmentAccessor,ILogger<EmailSender> logger
            ,IWebHostEnvironment environment, IHttpClientFactory httpClientFactory,IOptions<EmailAuth> emailAccessor, IOptions<AppEndpoint> appEndpoint)
        {
            _logger = logger;
            _environment = environment;
            _httpClientFactory = httpClientFactory;
            EmailAccessor = emailAccessor.Value;
            EnvironmentAccessor = environmentAccessor.Value;
            BaseUrl = appEndpoint.Value.APIUri.HealthBancFrontendBase;
        }


        public void SendUserVerificationMail(string email,string subject,string verificationUrl)
        {
            var path = Path.Combine(_environment.WebRootPath, "EmailTemplates") + "\\verify.html";
            string html = System.IO.File.ReadAllText(path);
            var newHtml =  html.Replace("token", verificationUrl).Replace("BaseUrl",BaseUrl);
            var emailRequest = new EmailRequest(email, newHtml, subject, "healthbanc@sterling.ng");
            EmailRequest(emailRequest);
        }
        public void SendUserResetPasswordMail(string email, string subject, string resetUrl)
        {
            var path = Path.Combine(_environment.WebRootPath, "EmailTemplates") + "\\password_reset.html";
            string html = System.IO.File.ReadAllText(path);
            var newHtml = html.Replace("token", resetUrl).Replace("BaseUrl", BaseUrl);
            var emailRequest = new EmailRequest(email, newHtml, subject, "healthbanc@sterling.ng");
            EmailRequest(emailRequest);
        }
        public void FamilySubscription(string email, string subject, string userName)
        {
            var path = Path.Combine(_environment.WebRootPath, "EmailTemplates\\FamilyInsurance") + "\\familysubscription.html";
            string html = System.IO.File.ReadAllText(path);
            string newHtml = html.Replace("UserName", userName).Replace("BaseUrl", BaseUrl);
            var emailRequest = new EmailRequest(email, newHtml, subject, "healthbanc@sterling.ng");
            EmailRequest(emailRequest);
        }
        public void SendHealthInsuredFamilyPaymentReminder(string email, string subject, string familyHead, string familyMember)
        {
            //var path = Path.Combine(_environment.WebRootPath, "EmailTemplates\\FamilyInsurance") + "\\family_healthinsured_paymentreminder.html";
            //string html = System.IO.File.ReadAllText(path);
            //var newHtml = html.Replace("FamilyHead", familyHead).Replace("FamilyMember", familyMember).Replace("BaseUrl", BaseUrl);
            //var emailRequest = new EmailRequest(email, newHtml, subject, "healthbanc@sterling.ng");
            //EmailRequest(emailRequest);
        }
        public void HealthInsuredSubscriptionMail(string email, string subject, string userName, string enroleeNumber, string healthCareProvider, string plan)
        {
            var path = Path.Combine(_environment.WebRootPath, "EmailTemplates\\IndividualInsurance") + "\\healthinsured_subscription.html";
            string html = System.IO.File.ReadAllText(path);
            string processedEnrolleNumber;
            if (enroleeNumber == "" || enroleeNumber == "Pending" || enroleeNumber == null)
            {
                processedEnrolleNumber = "pending and would be provided soon. This can be accessed via your dashbard";
            }
            else
            {
                processedEnrolleNumber = enroleeNumber;
            }

            string subType = plan == "1" ? "Ruby" : "Sapphire";
            string newHtml = html.Replace("UserName", userName).Replace("HealthServiceProviderName", healthCareProvider).Replace("EnroleeNumber", processedEnrolleNumber)
                .Replace("SubscriptionType", subType).Replace("BaseUrl", BaseUrl);

            var emailRequest = new EmailRequest(email, newHtml, subject, "healthbanc@sterling.ng");
            EmailRequest(emailRequest);
        }
        public void SendHealthInsuredPaymentReminder(string email, string subject, string userName,string info)
        {
            //var path = Path.Combine(_environment.WebRootPath, "EmailTemplates\\IndividualInsurance") + "\\healthinsured_paymentreminder.html";
            //string html = System.IO.File.ReadAllText(path);
            //var newHtml = html.Replace("UserName", userName).Replace("BaseUrl", BaseUrl).Replace("Info",info);
            //var emailRequest = new EmailRequest(email, newHtml, subject, "healthbanc@sterling.ng");
            //EmailRequest(emailRequest);
        }        
        public void SendHeliumNotification(string subject,string healthProvider,string providerType,string phonenumber,string providerEmail)
        {
            var path = Path.Combine(_environment.WebRootPath, "EmailTemplates") + "\\heliumNotification.html";
            string html = System.IO.File.ReadAllText(path);
            var newHtml = html.Replace("HealthServiceProviderName", healthProvider).Replace("PhoneNumber", phonenumber)
                .Replace("HealthServiveProviderType", providerType).Replace("EmailAddress", providerEmail).Replace("BaseUrl", BaseUrl);
            var emailRequest = new EmailRequest("healthbanc@sterling.ng", newHtml, subject, providerEmail);
            EmailRequest(emailRequest);
        }
        public void SendHealthFinanceNotification(string subject, HealthFinanceCollectionViewModel  healthFinance,List<string> toEmails)
        {
            var path = Path.Combine(_environment.WebRootPath, "EmailTemplates") + "\\healthfinance.html";
            string html = System.IO.File.ReadAllText(path);
            var newHtml = html.Replace("{Name}", healthFinance.Name).Replace("{BusinessAddress}", healthFinance.BusinessAddress)
                .Replace("{BusinessName}", healthFinance.BusinessName).Replace("{BusinessType}", healthFinance.BusinessType).Replace("{Email}", healthFinance.Email)
                .Replace("{Phonenumber}", healthFinance.Phonenumber).Replace("{Amount}", healthFinance.Amount).Replace("{Comment}", healthFinance.Comment).Replace("BaseUrl", BaseUrl);
            foreach(var item in toEmails)
            {
                var emailRequest = new EmailRequest(item, newHtml, subject, healthFinance.Email);
                EmailRequest(emailRequest);
            }           
        }
        public void SendHealthFinanceSubmissionNotification(string subject, HealthFinanceCollectionViewModel healthFinance, string  toEmails)
        {
            var path = Path.Combine(_environment.WebRootPath, "EmailTemplates") + "\\healthfinance_submission.html";
            string html = System.IO.File.ReadAllText(path);
            var newHtml = html.Replace("{Name}", healthFinance.Name).Replace("{BusinessAddress}", healthFinance.BusinessAddress)
                .Replace("{BusinessName}", healthFinance.BusinessName).Replace("{BusinessType}", healthFinance.BusinessType).Replace("{Email}", healthFinance.Email)
                .Replace("{Phonenumber}", healthFinance.Phonenumber).Replace("{Amount}", healthFinance.Amount).Replace("{Comment}", healthFinance.Comment).Replace("BaseUrl", BaseUrl);
            
            var emailRequest = new EmailRequest(healthFinance.Email, newHtml, subject, "healthbanc@sterling.ng");
            EmailRequest(emailRequest);
        }
        public void HealthInsuredFailedDebitNotification(string email, string subject, string userName,string info, string premium)
        {
            var path = Path.Combine(_environment.WebRootPath, "EmailTemplates\\IndividualInsurance") + "\\faileddebit_notification.html";
            string html = System.IO.File.ReadAllText(path);
            var newHtml = html.Replace("UserName", userName).Replace("PremiumAmount", premium).Replace("Info", info).Replace("BaseUrl", BaseUrl);
            var emailRequest = new EmailRequest(email, newHtml, subject, "healthbanc@sterling.ng");
            EmailRequest(emailRequest);
        }
        public void HealthInsuredDeactivationNotification(string email, string subject, string userName,string premium,string info)
        {
            var path = Path.Combine(_environment.WebRootPath, "EmailTemplates\\IndividualInsurance") + "\\deactivation_notification.html";
            string html = System.IO.File.ReadAllText(path);
            var newHtml = html.Replace("UserName", userName).Replace("PremiumAmount", premium).Replace("Info",info).Replace("BaseUrl", BaseUrl);
            var emailRequest = new EmailRequest(email, newHtml, subject, "healthbanc@sterling.ng");
            EmailRequest(emailRequest);
        }
        public void HealthInsuredFailedCompanyDebit(string email, string subject, string userName, string premium,string stopDate)
        {
            var path = Path.Combine(_environment.WebRootPath, "EmailTemplates\\CorporateInsurance") + "\\failedcompany_debit.html";
            string html = System.IO.File.ReadAllText(path);
            var newHtml = html.Replace("UserName", userName).Replace("PremiumAmount", premium).Replace("StopDate",stopDate).Replace("BaseUrl", BaseUrl);
            var emailRequest = new EmailRequest(email, newHtml, subject, "healthbanc@sterling.ng");
            EmailRequest(emailRequest);
        }
        public void HealthInsuredCompanyDeactivation(string email, string subject, string userName, string premium)
        {
            var path = Path.Combine(_environment.WebRootPath, "EmailTemplates\\CorporateInsurance") + "\\deactivate_companybeneficiaries.html";
            string html = System.IO.File.ReadAllText(path);
            var newHtml = html.Replace("UserName", userName).Replace("PremiumAmount", premium).Replace("BaseUrl", BaseUrl);
            var emailRequest = new EmailRequest(email, newHtml, subject, "healthbanc@sterling.ng");
            EmailRequest(emailRequest);
        }
        public void CorporateInsuranceOnboarding(string email, string subject, string otp)
        {
            var path = Path.Combine(_environment.WebRootPath, "EmailTemplates\\CorporateInsurance") + "\\corporate_onboarding.html";
            string html = System.IO.File.ReadAllText(path);
            var newHtml = html.Replace("OTPCODE", otp).Replace("BaseUrl", BaseUrl);
            var emailRequest = new EmailRequest(email, newHtml, subject, "healthbanc@sterling.ng");
            EmailRequest(emailRequest);
        }
        public void RefreeInvitation(string email,string subject,string payee)
        {
            var path = Path.Combine(_environment.WebRootPath, "EmailTemplates\\IndividualInsurance\\Payee") + "\\refereeinvitation.html";
            string html = System.IO.File.ReadAllText(path);
            var newHtml = html.Replace("Payee", payee).Replace("BaseUrl", BaseUrl);
            var emailRequest = new EmailRequest(email, newHtml, subject, "healthbanc@sterling.ng");
            EmailRequest(emailRequest);
        }
        public void RefreeInvitationFullDetail(string email, string subject, string payee,string userName, string enroleeNumber, string healthCareProvider, string plan)
        {
            var path = Path.Combine(_environment.WebRootPath, "EmailTemplates\\IndividualInsurance\\Payee") + "\\refereeInvitationFullDetails.html";
            string html = System.IO.File.ReadAllText(path);

            string processedEnrolleNumber;
            if (enroleeNumber == "" || enroleeNumber == "Pending" || enroleeNumber == null)
            {
                processedEnrolleNumber = "pending and would be provided soon. This can be accessed via your dashbard";
            }
            else
            {
                processedEnrolleNumber = enroleeNumber;
            }

            string subType = plan == "1" ? "Ruby" : "Sapphire";
            var newHtml = html.Replace("Payee", payee).Replace("UserName", userName).Replace("HealthServiceProviderName", healthCareProvider).Replace("EnroleeNumber", processedEnrolleNumber)
                .Replace("SubscriptionType", subType).Replace("BaseUrl", BaseUrl);
            var emailRequest = new EmailRequest(email, newHtml, subject, "healthbanc@sterling.ng");
            EmailRequest(emailRequest);
        }
        public void CustomMail(string email,string subject, string content)
        {
            string html = content;
            var emailRequest = new EmailRequest(email, html, subject, "healthbanc@sterling.ng");
            EmailRequest(emailRequest);
        }
        public async void EmailRequest(EmailRequest emailRequest)
        {
            _logger.LogInformation($"Email Request [ Subject :  {emailRequest.subject}]\n");
            var httpClient = _httpClientFactory.CreateClient("EmailSender");
            HttpContent content = new StringContent(JsonConvert.SerializeObject(emailRequest), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync($"{EmailAccessor.EmailNotificationNotify}", content);
            var apiResponse = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"Email Response : {apiResponse}\n");
        }
    }
}
