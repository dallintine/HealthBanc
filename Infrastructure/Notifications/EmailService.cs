using Application.CommonDTO;
using Application.Core.ConfigSettings;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Notifications
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger,IHttpClientFactory httpClientFactory)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<bool> EmailRequest(EmailRequest emailRequest)
        {
            _logger.LogInformation($"Email Request [ Subject :  {emailRequest.Subject} | Email : {emailRequest.Email}]\n");
            var httpClient = _httpClientFactory.CreateClient("EmailSender");
            HttpContent content = new StringContent(JsonConvert.SerializeObject(emailRequest), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync($"{_emailSettings.EmailNotificationNotify}", content);
            var apiResponse = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"Email Response : {apiResponse} | StatusCode : {response.StatusCode} \n");
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            return false;
        }
    }
}
