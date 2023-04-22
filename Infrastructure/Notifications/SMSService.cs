using Application.CommonDTO;
using Application.Core.ConfigSettings;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Infrastructure.Notifications
{
    public class SMSService : ISMSService
    {
        private readonly ILogger<SMSService> _logger;
        private SMSSettings SMSParameter;
        public SMSService(IOptions<SMSSettings> smsParameter, ILogger<SMSService> logger)
        {
            SMSParameter = smsParameter.Value;
            _logger = logger;
        }

        public async Task<BaseResponse> SendSmsAsync(string phone, string message)
        {
            _logger.LogInformation($"Sending SMS [Mobile : {phone}]\n");
            XmlDocument xmlDoc = new XmlDocument();
            var client = new HttpClient();
            client.BaseAddress = new Uri($"{SMSParameter.SmsBaseUrl}");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version='1.0' encoding='utf - 8'?>");
            sb.AppendLine("<soap12:Envelope xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:soap12='http://www.w3.org/2003/05/soap-envelope'>");
            sb.AppendLine("<soap12:Body>");
            sb.AppendLine("<SendSMS xmlns='http://tempuri.org/'>");
            sb.AppendLine($"<message>{message}</message>");
            sb.AppendLine($"<gsm>{phone}</gsm>");
            sb.AppendLine("</SendSMS>");
            sb.AppendLine("</soap12:Body>");
            sb.AppendLine("</soap12:Envelope>");
            var body = sb.ToString();

            HttpContent content = new StringContent(body, Encoding.UTF8, "application/soap+xml");
            var response = await client.PostAsync(SMSParameter.SmsUrl, content);
            var getResponse = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"Sending SMS Response [Response : {getResponse}]\n");
            if (response.IsSuccessStatusCode)
            {
                xmlDoc.LoadXml(getResponse);
                string? responseCode = xmlDoc.GetElementsByTagName("SendSMSResult").Item(0).InnerText;
                return BaseResponse.Success();
            }
            return BaseResponse.Failure("06","Could not send SMS, Please try again latter");
        }
    }
}