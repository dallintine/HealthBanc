using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Infrastructure.Mail
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

        public class HelloEmail
        {
            [JsonProperty("token")]
            public string token { get; set; }
            public string company { get; set; }
            public string HealthServiceProviderName { get; set; }
            public string HealthServiveProviderType { get; set; }
            public string EmailAddress { get; set; }
            public string PhoneNumber { get; set; }
            public string UserName { get; set; }
            public string PremiumAmount { get; set; }
        }
    }
}



  //"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",