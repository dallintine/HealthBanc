using Application.Core.ConfigSettings;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private readonly IHttpConnection _httpConnect;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, IHttpConnection httpConnect, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _httpConnect = httpConnect;
            _logger = logger;
        }

        public async Task<bool> EmailRequest(EmailRequest emailRequest)
        {
            _logger.LogInformation($"Email Request [ Subject :  {emailRequest.subject} | Email : {emailRequest.email}]\n");

            var request = new HttpRequestMessage(new HttpMethod("POST"), _emailSettings.EmailNotificationNotify);
            HttpContent content = new StringContent(emailRequest, Encoding.UTF8, "application/json");
            return await _httpConnect.DevAPIRequest<BaseResponse>(request, content, "EmailClient");

            var httpClient = _httpConnect.DevAPIRequest<>() _httpClientFactory.CreateClient("EmailSender");
            HttpContent content = new StringContent(JsonConvert.SerializeObject(emailRequest), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync($"{EmailAccessor.EmailNotificationNotify}", content);
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
