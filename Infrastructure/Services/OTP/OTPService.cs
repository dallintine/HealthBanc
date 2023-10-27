using Application.Common.DTO;
using Application.Common.ConfigSettings;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Xml;
using Newtonsoft.Json;
using Application.AdminAuth.Commands;
using Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.OTP
{
    public class OTPService : IOTPService
    {
        private readonly ILogger<OTPService> _logger;
        private readonly IEncryptionService _encryptionService;
        private readonly ApplicationDbContext _context;
        private readonly SterlingOtpSettings _otpSettings;
        private readonly AppEndpointSettings _appEndpoint;

        public OTPService(ILogger<OTPService> logger, IEncryptionService encryptionService, IOptions<AppEndpointSettings> appEndpoint,ApplicationDbContext context ,
            IOptions<SterlingOtpSettings>  otpSettings)
        {
            _logger = logger;
            _encryptionService = encryptionService;
            _context = context;
            _otpSettings = otpSettings.Value;
            _appEndpoint = appEndpoint.Value;
        }

        public async Task<BaseResponse<string>> CreateOTP(long userId, string action)
        {
            var otpCode = new Random().Next(100000, 999999).ToString();
            var userOTP = new OneTimePassword
            {
                Action = action,
                ApplicationUserId = userId,
                ExpiresAt = DateTime.Now.AddMinutes(7),
                Otp = _encryptionService.SHA512(otpCode)
            };
            _context.OneTimePasswords.Add(userOTP);
            await _context.SaveChangesAsync();
            return BaseResponse<string>.Success(otpCode);
        }

        public async Task<BaseResponse> ValidateOTP(long userId, string encryptedOTP, string action)
        {
            var hashedOTP = _encryptionService.SHA512(encryptedOTP);
            var otp = await _context.OneTimePasswords.Where(x => x.ApplicationUserId == userId && x.Action.ToLower() == action.ToLower() && x.Status == false)
               .OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync();
            if (otp != null)
            {
                if (otp.ExpiresAt > DateTimeOffset.Now)
                {
                    if (!otp.Status)
                    {
                        if (otp.Otp == hashedOTP)
                        {
                            otp.Status = true;
                            _context.OneTimePasswords.Update(otp);
                            await _context.SaveChangesAsync();
                            return BaseResponse.Success();
                        }
                        _logger.LogInformation($"Validate OTP Terminated [Reason : OTP is old | UserId : {userId} | Action : {action} ]\n");
                        return BaseResponse.Failure("12","OTP is invalid");
                    }
                    _logger.LogInformation($"Validate OTP Terminated [Reason : OTP has been used | UserId : {userId} | Action : {action} ]\n");
                    return BaseResponse.Failure("12", "OTP is invalid");
                }
                _logger.LogInformation($"Validate OTP Terminated [Reason : OTP has expired | UserId : {userId} | Action : {action} ]\n");
                return BaseResponse.Failure("54","OTP is invalid");
            }
            _logger.LogInformation($"Validate OTP Terminated [Reason : No OTP found | UserId : {userId} | Action : {action} ]\n");
            return BaseResponse.Failure("12","OTP is invalid");
        }

        public BaseResponse ValidateAdminOTPAuth(string otp, string username)
        {
            var checkOTP = SOAPManual(otp, username);
            if (checkOTP == "")
            {
                return BaseResponse.Failure("12", "Login details invalid, please try again with correct credentials");
            }
            if (checkOTP == "false")
            {
                return BaseResponse.Failure("06", "Could not connect with OTP Service");
            }
            return BaseResponse.Success();
        }

        private string SOAPManual(string otp, string username)
        {

            string url = _otpSettings.Url;
            string action = _otpSettings.Action;

            try
            {
                _logger.LogInformation($"Validate Admin OTP soap request processing [username : {username} | OTP : {otp}]");
                XmlDocument soapEnvelopeXml = CreateSoapEnvelope(otp, username);
                HttpWebRequest webRequest = CreateWebRequest(url, action);
                //webRequest.Host = "az-cpibap2-serv";

                using Stream stream = webRequest.GetRequestStream();
                soapEnvelopeXml.Save(stream);

                string result;
                using WebResponse response = webRequest.GetResponse();
                using StreamReader rd = new StreamReader(response.GetResponseStream());
                result = rd.ReadToEnd();
                _logger.LogInformation($"Validate Admin OTP soap response [Response : {result}]");
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(result);
                var resultResponse = xmlDoc.GetElementsByTagName("OtpValidationResult").Item(0).InnerText;
                return resultResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError("Could not connect to OTP Service " + ex.ToString());
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

        private XmlDocument CreateSoapEnvelope(string otp, string username)
        {
            XmlDocument soapEnvelopeXml = new XmlDocument();
            soapEnvelopeXml.LoadXml(@$"<?xml version=""1.0"" encoding=""utf-8""?>
                <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
                    <soap:Body>
                        <OtpValidation  xmlns=""http://tempuri.org/"">
                        <otp>{otp}</otp>
                        <username>{username}</username>
                        <hashkey>{_otpSettings.HashKey}</hashkey>
                        </OtpValidation>
                    </soap:Body>
                </soap:Envelope>");
            return soapEnvelopeXml;
        }
    }
}
