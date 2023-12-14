using Application.Common.ConfigSettings;
using Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.DTO;
using Microsoft.AspNetCore.Hosting;
using Domain.Enums;
using Hangfire;

namespace Infrastructure.Notifications
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<EmailService> _logger;
        private readonly AppEndpointSettings _appEndpointSettings;
        private readonly IHttpClientFactory _httpClientFactory;

        public EmailService(IOptions<EmailSettings> emailSettings, IWebHostEnvironment environment, ILogger<EmailService> logger,
            IOptions<AppEndpointSettings> appEndpointSettings,IHttpClientFactory httpClientFactory)
        {
            _emailSettings = emailSettings.Value;
            _environment = environment;
            _logger = logger;
            _appEndpointSettings = appEndpointSettings.Value;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<bool> EmailRequest(EmailRequest emailRequest)
        {
            _logger.LogInformation($"Email Request [ Subject :  {emailRequest.Subject} | Email : {emailRequest.Email}]\n");
            emailRequest.Message = emailRequest.Message.Replace("{{BaseUrl}}", _appEndpointSettings.FrontendBaseUrl).Replace("{{Year}}", DateTime.Now.Year.ToString())
                .Replace("{{SupportEmail}}",_emailSettings.SupportEmail).Replace("{{SupportPhonenumber}}",_emailSettings.SupportPhonenumber);

            var httpClient = _httpClientFactory.CreateClient("EmailClient");
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

        [AutomaticRetry(Attempts = 0)]
        public async Task PaymentConfirmationEmail(string userName,string email)
        {
            var path = Path.Combine(_environment.WebRootPath, "EmailTemplates") + "/paymentConfirmation.html";
            var htmlTemplate = File.ReadAllText(path);
            var emailTemplate = htmlTemplate.Replace("{{Name}}", userName);
            await EmailRequest(new EmailRequest
            {
                Subject = "Healthbanc Payment Confirmation",
                Message = emailTemplate,
                Email = email
            });
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task PlanStepsEmail(string userName, string email, string service, string vendorName , string productName)
        {
            string path = null;
            if (service == ServicesEnum.Physicals.ToString())
            {
                _logger.LogInformation("Sending physical plan steps");
                path = Path.Combine(_environment.WebRootPath, "EmailTemplates") + "/gym.html";
            }
            else if (service == ServicesEnum.HeathlyMeal.ToString())
            {
                _logger.LogInformation("Sending healthy meal plan steps");
                path = Path.Combine(_environment.WebRootPath, "EmailTemplates") + "/healthMeal.html";
            }
            else if (service == ServicesEnum.Diagnostics.ToString())
            {
                _logger.LogInformation("Sending diagnostics plan steps");
                path = Path.Combine(_environment.WebRootPath, "EmailTemplates") + "/diagnostics.html";
            }
            if (path != null)
            {
                string subject;
                if(service ==  ServicesEnum.Diagnostics.ToString())
                {
                    subject = $"Your Access to {vendorName} is Now Active! ";
                }
                else if (service == ServicesEnum.Diagnostics.ToString())
                {
                    subject = $"Next Steps For Your {service} Plan with Healthbanc\r\n";
                }
                else
                {
                    subject = $"Next Steps for Your {productName} Purchase on Healthbanc";
                }
                var htmlTemplate = File.ReadAllText(path);
                var emailTemplate = htmlTemplate.Replace("{{Name}}", userName).Replace("{{ProductName}}",productName).Replace("{{VendorName}}",vendorName);
                await EmailRequest(new EmailRequest
                {
                    Subject = subject,
                    Message = emailTemplate,
                    Email = email
                });
            }
            else
            {
                _logger.LogInformation("Plan step Path is null");
            }
        }
    }
}
