using Application.DTO;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Mail
{
    public class EmailSender : IEmailSender
    {
        private readonly ILogger<EmailSender> _logger;
        public EmailSender(ILogger<EmailSender> logger, IOptions<AuthMessageSenderOption>optionAccessor)
        {
            Options = optionAccessor.Value;
            _logger = logger;
        }
        public AuthMessageSenderOption Options { get; }

        public void SendEmail(string email,string templateId, string url,string companyName)
        {
            var sendGridClient = new SendGridClient(Options.SendGridApiKey);

            var sendGridMessage = new SendGridMessage();
            sendGridMessage.SetFrom("healthbancng@gmail.com", "HEALTHBANC");
            sendGridMessage.AddTo(email, "HEALTHBANC");
            sendGridMessage.SetTemplateId(templateId);
            sendGridMessage.SetTemplateData(new HelloEmail
            {
                token = url,
                company = companyName
            });

            var response = sendGridClient.SendEmailAsync(sendGridMessage).Result;
        }

        public void SendEmailWithObject(string email, string templateId, HelloEmail helloEmail)
        {
            var sendGridClient = new SendGridClient(Options.SendGridApiKey);

            var sendGridMessage = new SendGridMessage();
            sendGridMessage.SetFrom("healthbancng@gmail.com", "HEALTHBANC");
            sendGridMessage.AddTo(email, "HEALTHBANC");
            sendGridMessage.SetTemplateId(templateId);
            sendGridMessage.SetTemplateData(new HelloEmail
            {
                token = helloEmail.token,
                HealthServiceProviderName = helloEmail.HealthServiceProviderName,
                HealthServiveProviderType = helloEmail.HealthServiveProviderType,
                EmailAddress = helloEmail.EmailAddress,
                PhoneNumber = helloEmail.PhoneNumber
            });

            var response = sendGridClient.SendEmailAsync(sendGridMessage).Result;
        }

        public void SendInsurancePaymentReminder(string email, string templateId, string url, string userName,string premiumAmount)
        {
            var sendGridClient = new SendGridClient(Options.SendGridApiKey);

            var sendGridMessage = new SendGridMessage();
            sendGridMessage.SetFrom("healthbancng@gmail.com", "HEALTHBANC");
            sendGridMessage.AddTo(email, "HEALTHBANC");
            sendGridMessage.SetTemplateId(templateId);
            sendGridMessage.SetTemplateData(new HelloEmail
            {
                token = url,
                UserName = userName,
                PremiumAmount = premiumAmount
            });

            var response = sendGridClient.SendEmailAsync(sendGridMessage).Result;
        }
    }
}
