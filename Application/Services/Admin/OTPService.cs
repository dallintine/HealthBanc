using Application.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;

namespace Application.Services.Admin
{
    public class OTPService
    {
        private readonly ILogger<OTPService> _logger;

        private SterlingOtpConfig _otpConfigAccessor { get; }

        public OTPService(IOptions<SterlingOtpConfig> otpConfigAccessor,ILogger<OTPService> logger)
        {
            _otpConfigAccessor = otpConfigAccessor.Value;
            _logger = logger;
        }

        public string SOAPManual(string otp, string username)
        {

            string url = _otpConfigAccessor.Url;
            string action = _otpConfigAccessor.Action;

            try
            {
                XmlDocument soapEnvelopeXml = CreateSoapEnvelope(otp, username);
                HttpWebRequest webRequest = CreateWebRequest(url, action);
                webRequest.Host = "az-cpibap2-serv";

                using Stream stream = webRequest.GetRequestStream();
                soapEnvelopeXml.Save(stream);

                string result;
                using WebResponse response = webRequest.GetResponse();
                using StreamReader rd = new StreamReader(response.GetResponseStream());
                result = rd.ReadToEnd();
                //using (WebResponse response = webRequest.GetResponse())
                //{
                //    using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                //    {
                //        result = rd.ReadToEnd();
                //    }
                //}
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("This happened in the SoapManual: " + ex.ToString());
                return ex.ToString();
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

        private XmlDocument CreateSoapEnvelope(string otp, string username)
        {
            XmlDocument soapEnvelopeXml = new XmlDocument();
            soapEnvelopeXml.LoadXml(@$"<?xml version=""1.0"" encoding=""utf-8""?>
                <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
                    <soap:Body>
                        <OtpValidation  xmlns=""http://tempuri.org/"">
                        <otp>{otp}</otp>
                        <username>{username}</username>
                        <hashkey>{_otpConfigAccessor.Hashkey}</hashkey>
                        </OtpValidation>
                    </soap:Body>
                </soap:Envelope>");
            return soapEnvelopeXml;
        }
    }
}
