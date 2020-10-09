using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

        public BackendOTPService(IOptions<HealthBanc.Helpers.SterlingOtp> optionAccessor, ILogger<BackendOTPService> logger)
        {
            Options = optionAccessor.Value;
            _logger = logger;
        }

        public String SOAPManual(string otp, string username)
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

    }
}
