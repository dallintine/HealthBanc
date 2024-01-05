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
using Application.Payment.DTO;
using System.Text.Encodings.Web;
using System.Web;

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

        public async Task<BaseResponse> ValidateAdminOTPAuth(string otp, string username)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_otpSettings.Url}{_otpSettings.ValidateURL}?Pin={otp}&SecretCode={HttpUtility.UrlEncode(_otpSettings.SecretKey)}");
            var response = await client.SendAsync(request);
            if(response.StatusCode == HttpStatusCode.OK)
            {
                var apiResponse = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Google OTP response {apiResponse}");
                if (apiResponse.ToLower() == "false")
                {
                    return BaseResponse.Failure("12", "Login details invalid, please try again with correct credentials");
                }
                else if(apiResponse.ToLower() == "true")
                {
                    return BaseResponse.Success();
                }
                else
                {
                    return BaseResponse.Failure("06", "Could not connect with OTP Service");
                }
            }
            return BaseResponse.Failure("06", "Could not connect with OTP Service");
        }
    }
}
