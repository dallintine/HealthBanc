using Application.API_RequestModel;
using Application.DTO;
using Application.Helpers;
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
        private readonly IWebHostEnvironment _environment;
        private readonly IHttpClientFactory _httpClientFactory;

        public Application.Helpers.Environment EnvironmentAccessor { get; }
        private EmailAuth EmailAccessor { get; }
        public EmailSender(IOptions<Application.Helpers.Environment>environmentAccessor
            ,IWebHostEnvironment environment, IHttpClientFactory httpClientFactory,IOptions<EmailAuth> emailAccessor)
        {
            _environment = environment;
            _httpClientFactory = httpClientFactory;
            EmailAccessor = emailAccessor.Value;
            EnvironmentAccessor = environmentAccessor.Value;
        }


        public void SendUserVerificationMail(string email,string subject,string verificationUrl)
        {
            var path = Path.Combine(_environment.WebRootPath, "EmailTemplates") + "\\verify.html";
            string html = System.IO.File.ReadAllText(path);
            var newHtml =  html.Replace("token", verificationUrl);
            var emailRequest = new EmailRequest(email, newHtml, subject, "healthbanc@sterling.ng");
            EmailRequest(emailRequest);
        }

        public void SendUserResetPasswordMail(string email, string subject, string resetUrl)
        {
            var path = Path.Combine(_environment.WebRootPath, "EmailTemplates") + "\\password_reset.html";
            string html = System.IO.File.ReadAllText(path);
            var newHtml = html.Replace("token", resetUrl);
            var emailRequest = new EmailRequest(email, newHtml, subject, "healthbanc@sterling.ng");
            EmailRequest(emailRequest);
        }

        public void HealthInsuredSubscriptionMail(string email,string subject,string userName, string enroleeNumber, string healthCareProvider,string plan)
        {
            var path = Path.Combine(_environment.WebRootPath, "EmailTemplates") + "\\healthinsured_subscription.html";
            string html = System.IO.File.ReadAllText(path);
            string processedEnrolleNumber;
            if (enroleeNumber == "" || enroleeNumber == "Pending" || enroleeNumber == null)
            {
                processedEnrolleNumber = "pending and would be provided soon. This can be accessed via your dashbard";
            }
            else
            {
                processedEnrolleNumber =  enroleeNumber;
            }

            string subType = plan == "1" ? "Ruby" : "Sapphire";
            string newHtml = html.Replace("UserName", userName).Replace("HealthServiceProviderName", healthCareProvider).Replace("EnroleeNumber", processedEnrolleNumber)
                .Replace("SubscriptionType", subType);
            
            var emailRequest = new EmailRequest(email, newHtml, subject, "healthbanc@sterling.ng");
            EmailRequest(emailRequest);
        }

        public void SendHealthInsuredPaymentReminder(string email, string subject, string userName)
        {
            var path = Path.Combine(_environment.WebRootPath, "EmailTemplates") + "\\healthinsured_paymentreminder.html";
            string html = System.IO.File.ReadAllText(path);
            var newHtml = html.Replace("UserName", userName);
            var emailRequest = new EmailRequest(email, newHtml, subject, "healthbanc@sterling.ng");
            EmailRequest(emailRequest);
        }

        public void SendHeliumNotification(string subject,string healthProvider,string providerType,string phonenumber,string providerEmail)
        {
            var path = Path.Combine(_environment.WebRootPath, "EmailTemplates") + "\\heliumNotification.html";
            string html = System.IO.File.ReadAllText(path);
            var newHtml = html.Replace("HealthServiceProviderName", healthProvider).Replace("PhoneNumber", phonenumber)
                .Replace("HealthServiveProviderType", providerType).Replace("EmailAddress", providerEmail);
            var emailRequest = new EmailRequest("healthbanc@sterling.ng", newHtml, subject, providerEmail);
            EmailRequest(emailRequest);
        }

        public void SendHealthFinanceNotification(string subject, HealthFinanceCollectionViewModel  healthFinance,List<string> toEmails)
        {
            var path = Path.Combine(_environment.WebRootPath, "EmailTemplates") + "\\healthfinance.html";
            string html = System.IO.File.ReadAllText(path);
            var newHtml = html.Replace("{Name}", healthFinance.Name).Replace("{BusinessAddress}", healthFinance.BusinessAddress)
                .Replace("{BusinessName}", healthFinance.BusinessName).Replace("{BusinessType}", healthFinance.BusinessType).Replace("{Email}", healthFinance.Email)
                .Replace("{Phonenumber}", healthFinance.Phonenumber).Replace("{Amount}", healthFinance.Amount).Replace("{Comment}", healthFinance.Comment);
            foreach(var item in toEmails)
            {
                var emailRequest = new EmailRequest(item, newHtml, subject, healthFinance.Email);
                EmailRequest(emailRequest);
            }           
        }

        public void HealthInsuredFailedDebit(string email, string subject, string userName,string premium)
        {
            var path = Path.Combine(_environment.WebRootPath, "EmailTemplates") + "\\faileddebit.html";
            string html = System.IO.File.ReadAllText(path);
            var newHtml = html.Replace("UserName", userName).Replace("PremiumAmount", premium);
            var emailRequest = new EmailRequest(email, newHtml, subject, "healthbanc@sterling.ng");
            EmailRequest(emailRequest);
        }

        public void HealthInsuredFailedCompanyDebit(string email, string subject, string userName, string premium,string stopDate)
        {
            var path = Path.Combine(_environment.WebRootPath, "EmailTemplates") + "\\failedcompany_debit.html";
            string html = System.IO.File.ReadAllText(path);
            var newHtml = html.Replace("UserName", userName).Replace("PremiumAmount", premium).Replace("StopDate",stopDate);
            var emailRequest = new EmailRequest(email, newHtml, subject, "healthbanc@sterling.ng");
            EmailRequest(emailRequest);
        }

        public void HealthInsuredCompanyDeactivation(string email, string subject, string userName, string premium)
        {
            var path = Path.Combine(_environment.WebRootPath, "EmailTemplates") + "\\deactivate_companybeneficiaries.html";
            string html = System.IO.File.ReadAllText(path);
            var newHtml = html.Replace("UserName", userName).Replace("PremiumAmount", premium);
            var emailRequest = new EmailRequest(email, newHtml, subject, "healthbanc@sterling.ng");
            EmailRequest(emailRequest);
        }

        public void CorporateInsuranceOnboarding(string email, string subject, string otp)
        {
            var path = Path.Combine(_environment.WebRootPath, "EmailTemplates") + "\\corporate_onboarding.html";
            string html = System.IO.File.ReadAllText(path);
            var newHtml = html.Replace("OTPCODE", otp);
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
            var httpClient = _httpClientFactory.CreateClient("EmailSender");
            HttpContent content = new StringContent(JsonConvert.SerializeObject(emailRequest), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync($"{EmailAccessor.EmailNotificationNotify}", content);

            await response.Content.ReadAsStringAsync();
        }
    }
}
