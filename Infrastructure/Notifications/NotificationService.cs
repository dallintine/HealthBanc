using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Notifications
{
    public class NotificationService : INotificationService
    {
        private readonly DevApiSettings _devApiConfig;
        private readonly EmailSettings _emailSettings;
        private readonly IHttpConnection _httpConnect;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(IOptions<DevApiSettings> devApiConfig, IOptions<EmailSettings> emailSettings, IHttpConnection httpConnect, ILogger<NotificationService> logger)
        {
            _devApiConfig = devApiConfig.Value;
            _emailSettings = emailSettings.Value;
            _httpConnect = httpConnect;
            _logger = logger;
        }

        public async Task<BaseResponse> Email(EmailNotificationDTO emailNotificationModel)
        {
            var request = new HttpRequestMessage(new HttpMethod("POST"), _emailSettings.SendEmail);
            var multipartContent = new MultipartFormDataContent();
            foreach (var item in emailNotificationModel.To)
            {
                multipartContent.Add(new StringContent(item), "To");
            }
            if (emailNotificationModel.Cc != null)
            {
                foreach (var item in emailNotificationModel.Cc)
                {
                    multipartContent.Add(new StringContent(item), "Cc");
                }
            }
            if (emailNotificationModel.Bcc != null)
            {
                foreach (var item in emailNotificationModel.Bcc)
                {
                    multipartContent.Add(new StringContent(item), "Bcc");
                }
            }
            if (emailNotificationModel.Attchements != null)
            {
                foreach (var item in emailNotificationModel.Attchements)
                {
                    multipartContent.Add(new StreamContent(new MemoryStream(item.Attachmentfile)), "Attachment", item.FileName);
                }
            }

            multipartContent.Add(new StringContent(emailNotificationModel.Body), "Body");
            multipartContent.Add(new StringContent(emailNotificationModel.Subject), "Subject");
            return await _httpConnect.DevAPIRequest<BaseResponse>(request, multipartContent, "EmailClient");
        }

        public async Task<BaseResponse> SMS(SMSNotificationDTO notificationModel)
        {
            var sendSMS = new SendSMSRequestDTO
            {
                SenderId = _devApiConfig.ProductKeys.SenderId,
                Channel = _devApiConfig.ProductKeys.Channel,
                BraCode = _devApiConfig.ProductKeys.BRACode,
                Message = notificationModel.Message,
                ForAcid = notificationModel.ForAcid,
                MobileId = notificationModel.MobileId
            };
            var smsPayload = JsonConvert.SerializeObject(sendSMS);
            _logger.LogInformation($"Sending SmS notification [Payload : {smsPayload} ] \n");
            using var request = new HttpRequestMessage(new HttpMethod("POST"), _devApiConfig.SendSms);
            HttpContent content = new StringContent(smsPayload, Encoding.UTF8, "application/json");
            return await _httpConnect.DevAPIRequest<BaseResponse>(request, content, "DevApiClient");
        }
    }
}
