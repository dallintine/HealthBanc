using Application.DTO;
using Application.Helpers;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Infrastructure.SMS
{
    public class SMSService : ISMSService
    {
        private readonly ILogger<SMSService> _logger;
        private SMSParameter SMSParameter;
        public SMSService(IOptions<SMSParameter> smsParameter,ILogger<SMSService> logger)
        {
            SMSParameter = smsParameter.Value;
            _logger = logger;
        }

        public async Task<ResponseMessage> SendSmsAsync(string phone, string message)
        {
            _logger.LogInformation($"Sending SMS [Mobile : {phone} | Message : {message}]\n");
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
                var responseCode = xmlDoc.GetElementsByTagName("SendSMSResult").Item(0).InnerText;
                return new ResponseMessage { ResponseCode = 00 , Message="Approved or Completed Successfully" , Status=true };
            }
            return new ResponseMessage { ResponseCode = 21 , Message ="Could not send OTP, Please try again latter" };
        }
    }
}
