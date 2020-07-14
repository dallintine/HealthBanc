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

        public void SendEmail(string email,string templateId, string url)
        {
            var has = Options.SendGridApiKey;
            var sendGridClient = new SendGridClient(Options.SendGridApiKey);

            var sendGridMessage = new SendGridMessage();
            sendGridMessage.SetFrom("hassan.olatade.hh@gmail.com", "PHARMHUB");
            sendGridMessage.AddTo(email, "PHARMHUB");
            sendGridMessage.SetTemplateId(templateId);
            sendGridMessage.SetTemplateData(new HelloEmail
            {
                token = url,
            });

            var response = sendGridClient.SendEmailAsync(sendGridMessage).Result;
        }

        private class HelloEmail
        {
            [JsonProperty("token")]
            public string token { get; set; }
        }
    }
}
