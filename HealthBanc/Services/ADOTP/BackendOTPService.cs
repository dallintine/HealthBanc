using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceReference1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Xml;

namespace HealthBanc.Services.ADOTP
{
    public class BackendOTPService : IBackendOTPService
    {
        public readonly HealthBanc.Helpers.SterlingOtp Options;
        private readonly ILogger<BackendOTPService> _logger;
        public readonly string serviceUrl = "https://az-cpibap2-serv/OTPCentralService.asmx";
        public readonly EndpointAddress endpointAddress;
        public readonly BasicHttpBinding basicHttpBinding;

        public BackendOTPService(IOptions<HealthBanc.Helpers.SterlingOtp> optionAccessor, ILogger<BackendOTPService> logger)
        {

            Options = optionAccessor.Value;
            _logger = logger;
            endpointAddress = new EndpointAddress(serviceUrl);

            basicHttpBinding =
                new BasicHttpBinding(endpointAddress.Uri.Scheme.ToLower() == "http" ?
                            BasicHttpSecurityMode.None : BasicHttpSecurityMode.Transport);

            //Please set the time accordingly, this is only for demo
            basicHttpBinding.OpenTimeout = TimeSpan.MaxValue;
            basicHttpBinding.CloseTimeout = TimeSpan.MaxValue;
            basicHttpBinding.ReceiveTimeout = TimeSpan.MaxValue;
            basicHttpBinding.SendTimeout = TimeSpan.MaxValue;
            basicHttpBinding.UseDefaultWebProxy = true;
        }

        public string SOAPManual(string otp, string username)
        {
            const string url = "https://az-cpibap2-serv/OTPCentralService.asmx";
            const string action = "http://tempuri.org/OtpValidation";

            try
            {
                XmlDocument soapEnvelopeXml = CreateSoapEnvelope(otp,username);
                HttpWebRequest webRequest = CreateWebRequest(url, action);

                using (Stream stream = webRequest.GetRequestStream())
                {
                    soapEnvelopeXml.Save(stream);
                }

                string result;
                using (WebResponse response = webRequest.GetResponse())
                {
                    using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                    {
                        result = rd.ReadToEnd();
                    }
                }

                using (WebResponse response = webRequest.GetResponse())
                {
                    using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                    {
                        result = rd.ReadToEnd();
                    }
                }
                return result;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.ToString());
                return "false";
            }
            
        }

        private static HttpWebRequest CreateWebRequest(string url, string action)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Headers.Add(action);
            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";
            return webRequest;
        }

        private XmlDocument CreateSoapEnvelope(string otp,string username)
        {
            XmlDocument soapEnvelopeXml = new XmlDocument();
            soapEnvelopeXml.LoadXml(@$"<?xml version=""1.0"" encoding=""utf-8""?>
                <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
                    <soap:Body>
                        <OtpValidation  xmlns=""http://tempuri.org/"">
                        <otp>{otp}</otp>
                        <username>{username}</username>
                        <hashkey>{Options.SterlingOtpConfig.Hashkey}</hashkey>
                        </OtpValidation>
                    </soap:Body>
                </soap:Envelope>");
            return soapEnvelopeXml;
        }

        private async Task<OTPCentralServiceSoapClient> GetInstanceAsync()
        {
            return await Task.Run(() => new OTPCentralServiceSoapClient(basicHttpBinding, endpointAddress));
        }

        public async Task<OtpValidationResponse> OtpValidationAsync(string otp, string username)
        {
            var response2 = new OtpValidationResponse();
            try
            {
                var client = await GetInstanceAsync();
                var x = new OtpValidationRequestBody(otp, username, Options.SterlingOtpConfig.Hashkey);
                var response = await client.OtpValidationAsync(otp, username, Options.SterlingOtpConfig.Hashkey);
                _logger.LogError(response.ToString());
                return response;
            }
            catch(Exception ex)
            {
                _logger.LogCritical(ex.ToString(),ex.ToString());
            }
            return response2;
            
        }
    }
}
