using Application.DTO;
using Application.Helpers;
using Application.Interfaces;
using DataAccess;
using Domain.Enums;
using Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class OtpService
    {
        private readonly IUniqueIdentifier _uniqueIdentifier;
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly IEncryptAndDecrypt _encryptAndDecrypt;
        private readonly SterlingOtpConfig _otpConfigAccessor;
        private readonly ISMSService _smsService;
        private readonly ILogger<OtpService> _logger;
        private readonly IEmailSender _emailSender;

        public OtpService(IUniqueIdentifier uniqueIdentifier ,IRepositoryWrapper repositoryWrapper, IEncryptAndDecrypt encryptAndDecrypt, IOptions<SterlingOtpConfig> otpConfigAccessor,
            ISMSService smsService,ILogger<OtpService> logger,IEmailSender emailSender)
        {
            _uniqueIdentifier = uniqueIdentifier;
            _repositoryWrapper = repositoryWrapper;
            _encryptAndDecrypt = encryptAndDecrypt;
            _otpConfigAccessor = otpConfigAccessor.Value;
            _smsService = smsService;
            _logger = logger;
            _emailSender = emailSender;
        }

        public async Task<ResponseMessage> GenerateOtp(string phoneNumber,string email, int userId, string action, string app)
        {
            string generateOtpCode = _uniqueIdentifier.GetUniqueCode((int)_otpConfigAccessor.Length);


            string otpMessageTemplate = _otpConfigAccessor.OtpMessage;
            string otpMessage = otpMessageTemplate.Replace("{OTPCode}", generateOtpCode).Replace("{Action}",action).Replace("{App}", app);
            var saveotp = new OtpValidation()
            {
                OTP = _encryptAndDecrypt.Sha512Hash(generateOtpCode),
                PhoneNumber = phoneNumber,
                GeneratedDate = DateTimeOffset.Now,
                ExpiredDate = DateTimeOffset.Now.AddMinutes(_otpConfigAccessor.ExpiryTime),
                ApplicationUserId = userId,
                Email = email,
                Status = true,
                Action = action
            };
            _repositoryWrapper.OtpValidation.Create(saveotp);
            await _repositoryWrapper.Save();

            // Email the user the verification code
            if ( app is null || app.ToLower() == HealthbancApps.HealthInsured.ToString().ToLower())
            {
                await _smsService.SendSmsAsync(phoneNumber, otpMessage);
                await _emailSender.CustomHealthInsuredMail(email, "OTP Code", otpMessage);
            }
            else
            {
                await _smsService.SendSmsAsync(phoneNumber, otpMessage);
                await _emailSender.CustomHealthInsuredMail(email, "OTP Code", otpMessage);
            }
           
            _logger.LogInformation($"Generate OTP SMS feature completed [UserId : {userId}]");
            return new ResponseMessage { ResponseCode = 00, Message = "Approved or Completed Successfully", Status = true };
        }

        public async Task<ResponseMessage<OtpValidation>> ValidateOtp(int userID, string otp, string action)
        {
            _logger.LogInformation($"Processing Validate OTP Payload [UserId :{userID} | Action : {action} ]\n");
            var otpValidation = await _repositoryWrapper.OtpValidation.GetUserLastOTP(userID);
            if (otpValidation == null)
            {
                _logger.LogInformation($"Processing Validate OTP Terminated [Reason : No Record Found : OTP does not exist]\n");
                return new ResponseMessage<OtpValidation> { ResponseCode = 25, Message = "No Record Found : OTP does not exist" };
            }
            var now = DateTimeOffset.Now;
            var timeDifference = (now - otpValidation.GeneratedDate).Minutes;
            if (timeDifference > (int)_otpConfigAccessor.ExpiryTime)
            {
                _logger.LogInformation($"Processing Validate OTP Terminated [Reason : OTP Code has expired, Please try again]\n");
                return new ResponseMessage<OtpValidation> { ResponseCode = 21, Message = "OTP Code has expired, Please try again" };
            }
            if (_encryptAndDecrypt.Sha512Hash(otp) == otpValidation.OTP && otpValidation.Action.ToLower() == action.ToLower())
            {
                _logger.LogInformation($"Processing Validate OTP Successful \n");
                otpValidation.Status = false;
                _repositoryWrapper.OtpValidation.Update(otpValidation);
                await _repositoryWrapper.Save();
                return new ResponseMessage<OtpValidation> { ResponseCode = 00, Message = "Approved or Completed successfully", Data = otpValidation, Status = true };
            }
            _logger.LogInformation($"Processing Validate OTP Terminated [No Action Taken : OTP code does not match records]\n");
            return new ResponseMessage<OtpValidation> { ResponseCode = 21, Message = "No Action Taken : OTP code does not match  existing records" };
        }

    }
}
