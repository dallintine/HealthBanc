using Application.CommonDTO;
using Application.Core.ConfigSettings;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.OTP
{
    public class OTPService : IOTPService
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly ILogger<OTPService> _logger;
        private readonly IEncryptionService _encryptionService;
        private readonly AppEndpointSettings _appEndpoint;

        public OTPService(IRepositoryWrapper repositoryWrapper, ILogger<OTPService> logger, IEncryptionService encryptionService, IOptions<AppEndpointSettings> appEndpoint)
        {
            _repositoryWrapper = repositoryWrapper;
            _logger = logger;
            _encryptionService = encryptionService;
            _appEndpoint = appEndpoint.Value;
        }

        public async Task<BaseResponse<string>> CreateOTP(long userId, string action)
        {
            var otpCode = new Random().Next(1000, 9999).ToString();
            var userOTP = new OneTimePassword
            {
                Action = action,
                ApplicationUserId = userId,
                ExpiresAt = DateTime.Now.AddMinutes(7),
                Otp = _encryptionService.SHA512(otpCode)
            };
            _repositoryWrapper.OneTimePassword.Create(userOTP);
            await _repositoryWrapper.Save();
            return BaseResponse<string>.Success(otpCode);
        }

        public async Task<BaseResponse> ValidateOTP(string userId, string encryptedOTP, string action)
        {
            var decryptOTP = _encryptionService.DecryptString(encryptedOTP);
            if (!decryptOTP.Item1)
            {
                _logger.LogInformation($"Validate OTP terminated  [Reason : OTP not valid] \n");
                return BaseResponse.Failure("30",decryptOTP.Item2);
            }

            var decryptedOTP = decryptOTP.Item2;
            var hashedOTP = _encryptionService.SHA512(decryptedOTP);
            var otp = await _repositoryWrapper.OneTimePassword.GetUserLastOTP(userId, action);
            if (otp != null)
            {
                if (otp.ExpiresAt > DateTimeOffset.Now)
                {
                    if (!otp.Status)
                    {
                        if (otp.OTP == hashedOTP)
                        {
                            otp.Status = true;
                            _repositoryWrapper.OneTimePassword.Update(otp);
                            await _repositoryWrapper.Save();
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
    }
}
