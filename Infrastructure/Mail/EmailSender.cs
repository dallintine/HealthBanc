using Application.DTO;
using Application.Helpers;
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
        public Application.Helpers.Environment _environmentAccessor { get; }
        public AuthMessageSenderOption Options { get; }
        private SendGridTemplateId _emailTemplateAccessor { get; }
        private SendGridProductionTempateId _productionEmailTemplateAccessor { get; }

        public EmailSender(ILogger<EmailSender> logger, IOptions<AuthMessageSenderOption>optionAccessor,IOptions<Application.Helpers.Environment>environmentAccessor,
             IOptions<SendGridTemplateId> emailTemplateAccessor, IOptions<SendGridProductionTempateId> productionEmailTemplateAccessor)
        {
            Options = optionAccessor.Value;
            _logger = logger;
            _environmentAccessor = environmentAccessor.Value;
            _emailTemplateAccessor = emailTemplateAccessor.Value;
            _productionEmailTemplateAccessor = productionEmailTemplateAccessor.Value;
        }

        public void SendEmail(string email,string templateId, string url,string companyName)
        {
            var fromEmail = _environmentAccessor.Staging ? "healthbancng@gmail.com" : "healthbancng@sterling.ng";
            var apiKey = _environmentAccessor.Staging ? Options.SendGridApiKey : Options.SendGridProductionApiKey;

            var sendGridClient = new SendGridClient(apiKey);
            var sendGridMessage = new SendGridMessage();
            sendGridMessage.SetFrom(fromEmail, "HEALTHBANC");
            sendGridMessage.AddTo(email, "HEALTHBANC");
            sendGridMessage.SetTemplateId(templateId);
            sendGridMessage.SetTemplateData(new HelloEmail
            {
                token = url,
                company = companyName
            });

            var response = sendGridClient.SendEmailAsync(sendGridMessage).Result;
        }

        public void SendEmailWithObject(HelloEmail helloEmail)
        {
            var fromEmail = _environmentAccessor.Staging ? "healthbancng@gmail.com" : "healthbancng@sterling.ng";
            var email = _environmentAccessor.Staging ? "healthbancng@sterling.ng" : "healthbancng@gmail.com";
            var apiKey = _environmentAccessor.Staging ? Options.SendGridApiKey : Options.SendGridProductionApiKey;
            var templateId = _environmentAccessor.Staging ? _emailTemplateAccessor.HeliumHealth : _productionEmailTemplateAccessor.HeliumHealth;

            var sendGridClient = new SendGridClient(apiKey);
            var sendGridMessage = new SendGridMessage();
            sendGridMessage.SetFrom(fromEmail, "HEALTHBANC");
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

        public void SendInsurancePaymentReminder(string email,string userName)
        {
            var fromEmail = _environmentAccessor.Staging ? "healthbancng@gmail.com" : "healthbancng@sterling.ng";
            var apiKey = _environmentAccessor.Staging ? Options.SendGridApiKey : Options.SendGridProductionApiKey;
            var templateId = _environmentAccessor.Staging ? _emailTemplateAccessor.HealthInsured_PaymentReminder : _productionEmailTemplateAccessor.HealthInsured_PaymentReminder;

            var sendGridClient = new SendGridClient(apiKey);
            var sendGridMessage = new SendGridMessage();
            sendGridMessage.SetFrom(fromEmail, "HEALTHBANC");
            sendGridMessage.AddTo(email, "HEALTHBANC");
            sendGridMessage.SetTemplateId(templateId);
            sendGridMessage.SetTemplateData(new HelloEmail
            {
                UserName = userName,
            });

            var response = sendGridClient.SendEmailAsync(sendGridMessage).Result;
        }
    }
}
