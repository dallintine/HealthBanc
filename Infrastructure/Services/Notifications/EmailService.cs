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
            emailRequest.Message = emailRequest.Message.Replace("{{BaseUrl}}", _appEndpointSettings.FrontendBaseUrl).Replace("{{Year}}", DateTime.Now.Year.ToString());
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
            var path = Path.Combine(_environment.WebRootPath, "EmailTemplates") + "\\paymentConfirmation.html";
            var htmlTemplate = File.ReadAllText(path);
            var emailTemplate = htmlTemplate.Replace("{{Name}}", userName);
            await EmailRequest(new EmailRequest
            {
                Subject = "Healtbanc Payment Confirmation",
                Message = emailTemplate,
                Email = email
            });
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task PlanStepsEmail(string userName, string email,string service)
        {
            string path;
            if(service == ServicesEnum.Gym.ToString())
            {
                path = Path.Combine(_environment.WebRootPath, "EmailTemplates") + "\\gym.html";
            }
            else if (service == ServicesEnum.Meal.ToString())
            {
                path = Path.Combine(_environment.WebRootPath, "EmailTemplates") + "\\healthMeal.html";
            }
            else
            {
                path = Path.Combine(_environment.WebRootPath, "EmailTemplates") + "\\diagnostics.html";
            }
            var htmlTemplate = File.ReadAllText(path);
            var emailTemplate = htmlTemplate.Replace("{{Name}}", userName);
            await EmailRequest(new EmailRequest
            {
                Subject = "Healtbanc Payment Confirmation",
                Message = emailTemplate,
                Email = email
            });
        }
    }
}
