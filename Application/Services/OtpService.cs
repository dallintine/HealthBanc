using Application.DTO;
using Application.Interfaces;
using DataAccess;
using Domain.Models;
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

        public OtpService(IUniqueIdentifier uniqueIdentifier ,IRepositoryWrapper repositoryWrapper)
        {
            _uniqueIdentifier = uniqueIdentifier;
            _repositoryWrapper = repositoryWrapper;
        }

        private async Task<ResponseMessage> GenerateOtp(string phoneNumber,string email, int userId, string action,double expiryTime)
        {
            string generateOtpCode = _uniqueIdentifier.GetUniqueCode(6);
            var otp = await _repositoryWrapper.OtpValidation.GetUserLastOTP(userId);
            if (otp is null)
            {
                var saveotp = new OtpValidation()
                {
                    OTP = generateOtpCode,
                    PhoneNumber = phoneNumber,
                    Email = email,
                    GeneratedDate = DateTimeOffset.Now,
                    ExpiredDate = DateTimeOffset.Now.AddMinutes(expiryTime),
                    ApplicationUserId = userId,
                    Status = true,
                    Action = action
                };
                _repositoryWrapper.OtpValidation.Create(saveotp);
            }
            else
            {
                otp.OTP = generateOtpCode;
                otp.PhoneNumber = phoneNumber;
                otp.GeneratedDate = DateTimeOffset.Now;
                otp.ExpiredDate = DateTimeOffset.Now.AddMinutes(expiryTime);
                otp.ApplicationUserId = userId;
                otp.Status = true;
                otp.Action = action;
                _repositoryWrapper.OtpValidation.Update(otp);
            }
            await _repositoryWrapper.Save();
            return new ResponseMessage { ResponseCode = 00, Message = "Approved or Completed Successfully", Status = true };
        }
    }
}
