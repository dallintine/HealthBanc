using Application.API_RequestModel.Wallet;
using Application.API_ResponseModel.Wallet;
using Application.DTO;
using Application.Helpers;
using Application.Interfaces;
using DataAccess;
using Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Wallet
{
    public class WalletService


    {
        private readonly WalletConnect _walletConnect;
        private readonly ILogger<WalletService> _logger;
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly IUniqueIdentifier _uniqueIdentifier;
        private readonly IWalletEncryptionsAndDecryption _encryptionsAndDecryption;
        private readonly ISMSService _smsService;
        private readonly WalletSettings _walletSettings;

        public WalletService(WalletConnect walletConnect , ILogger<WalletService> logger,IRepositoryWrapper repositoryWrapper, IUniqueIdentifier uniqueIdentifier,
            IWalletEncryptionsAndDecryption encryptionsAndDecryption,ISMSService smsService, IOptions<WalletSettings> WalletSettings)
        {
            _walletConnect = walletConnect;
            _logger = logger;
            _repositoryWrapper = repositoryWrapper;
            _uniqueIdentifier = uniqueIdentifier;
            _encryptionsAndDecryption = encryptionsAndDecryption;
            _smsService = smsService;
            _walletSettings = WalletSettings.Value;
        }

        public async Task<ResponseMessage> GenerateOTPForWallet(int userId, string mobileNumber)
        {
            _logger.LogInformation($"Processing GenerateOTPForWallet Payload [UserId :{userId} | MobileNumber : {mobileNumber}]\n");
            var walletData = new GetWalletDetails(mobileNumber);
            var encryptData = _encryptionsAndDecryption.Encrypt(JsonConvert.SerializeObject(walletData));
            var validateWalletResponse = await _walletConnect.WalletDetails(encryptData);
            var decryptedResponse = _encryptionsAndDecryption.Decrypt(validateWalletResponse);
            _logger.LogInformation($"Validate Wallet decrypted response : {decryptedResponse}");
            var response = JsonConvert.DeserializeObject<ApiResponse<WalletValidationResponse>>(decryptedResponse);
            if (response != null && response.Response == "00")
            {
                return await GenerateOtp(mobileNumber, userId);
            }
            return new ResponseMessage { Message = response.Message, ResponseCode = 21 };
        }

        public async Task<ResponseMessage> ValidateOTP(int userID, string otp)
        {
            _logger.LogInformation($"Processing Validate Wallet Payload [UserId :{userID} | OTP : {otp}]\n");
            var otpValidation = await _repositoryWrapper.OtpValidation.GetUserLastOTP(userID);
            if (otpValidation == null)
            {
                _logger.LogInformation($"Processing Validate Wallet Terminated [Reason : No Record Found : OTP does not exist]\n");
                return new ResponseMessage { ResponseCode = 25, Message = "No Record Found : OTP does not exist" };
            }

            var now = DateTimeOffset.Now;
            var timeDifference = (now - otpValidation.GeneratedDate).Minutes;
            if (timeDifference > 7)
            {
                _logger.LogInformation($"Processing Validate Wallet Terminated [Reason : OTP Code has expired, Please try again]\n");
                return new ResponseMessage { ResponseCode = 21, Message = "OTP Code has expired, Please try again" };
            }
            if (otp == otpValidation.OTP)
            {
                _logger.LogInformation($"Processing Validate Wallet Successful \n");
                return new ResponseMessage { ResponseCode = 00, Message = "Approved or Completed successfully", Data = otpValidation, Status = true };
            }
            _logger.LogInformation($"Processing Validate Wallet Terminated [No Action Taken : OTP code does not match records]\n");
            return new ResponseMessage { ResponseCode = 21, Message = "No Action Taken : OTP code does not match records" };
        }

        public async Task<ResponseMessage> CreateWallet(int userId, string mobileNumber)
        {
            _logger.LogInformation($"Processing CreateWallet [UserId :{userId} | MobileNumber : {mobileNumber}]\n");
            var walletData = new GetWalletDetails(mobileNumber);
            var encryptData = _encryptionsAndDecryption.Encrypt(JsonConvert.SerializeObject(walletData));
            var validateWalletResponse = await _walletConnect.WalletDetails(encryptData);
            var decryptedResponse = _encryptionsAndDecryption.Decrypt(validateWalletResponse);
            _logger.LogInformation($"Validate Wallet decrypted response for creating wallet : {decryptedResponse}");
            var response = JsonConvert.DeserializeObject<ApiResponse<WalletValidationResponse>>(decryptedResponse);
            if (response != null && response.Response == "00")
            {
                return new ResponseMessage { ResponseCode = 21, Message = "Invalid Request - Wallet exist for this mobile number" };
            }
            var profile = await _repositoryWrapper.InsuranceProfile.GetByUserIdAsync(userId);
            var createWalletData = new CreateWallet
            {
                Firstname = profile.Othernames,
                Lastname = profile.Surname,
                Mobile = mobileNumber,
                DOB = profile.DateOfBirth,
                CURRENCYCODE = "NGN",
                AccountTier = ""
            };
            var payload = JsonConvert.SerializeObject(createWalletData);
            _logger.LogInformation($"Create Wallet Payload [Payload : {payload}]\n");
            var encryptCreatWalletData = _encryptionsAndDecryption.Encrypt(payload);
            var createWalletResponse = await _walletConnect.CreateWallet(encryptCreatWalletData);
            var decryptedCreateWalletResponse = _encryptionsAndDecryption.Decrypt(createWalletResponse);
            _logger.LogInformation($"Create Wallet decrypted response : {decryptedCreateWalletResponse}");
            var walletResponse = JsonConvert.DeserializeObject<ApiResponse<WalletValidationResponse>>(decryptedResponse);
            return new ResponseMessage { Status = true};
        }

        public async Task<ResponseMessage> WalletToSterling(int userId, decimal amount, string mobileNumber, string channel)
        {           
            var wallettransfer = new WalletToAccount
            {
                CURRENCYCODE = "NGN",
                ChannelID = int.Parse(_walletSettings.ChannelID),
                Toacct = _walletSettings.Toacct,
                PaymentRef = Guid.NewGuid().ToString(),
                Amt = amount.ToString(),
                TransferType = int.Parse(_walletSettings.TransferType),
                Remarks = "HealthInsured Payment",
                Frmacct = mobileNumber,
            };
            var payload = JsonConvert.SerializeObject(wallettransfer);
            _logger.LogInformation($"Wallet to Sterling payload [Payload : {payload} ]\n");
            var encryptData = _encryptionsAndDecryption.Encrypt(payload);
            var transferResponse = await _walletConnect.CreateWallet(encryptData);
            var decryptedResponse = _encryptionsAndDecryption.Decrypt(transferResponse);
            _logger.LogInformation($"Wallet To Account decrypted response : {decryptedResponse}");

            var response = JsonConvert.DeserializeObject<ApiResponse<WalletToAccountResponse>>(decryptedResponse);
            var paymentReference = new PaymentReference()
            {
                UserId = userId,
                PaymentMethod = PaymentMethod.Wallet.ToString(),
                Channel = channel,
                Refernce = wallettransfer.PaymentRef,
                Amount = amount
            };
            if (response.Data.Sent)
            {
                paymentReference.Status = PaymentReference_StatusValue.Successful.ToString();
                _repositoryWrapper.PaymentReference.Create(paymentReference);
                await _repositoryWrapper.Save();
                return new ResponseMessage { ResponseCode = 00, Message = "Approved or completed Successfully", Status = true };
            }
            else
            {
                paymentReference.Status = PaymentReference_StatusValue.Failed.ToString();
                _repositoryWrapper.PaymentReference.Create(paymentReference);
                await _repositoryWrapper.Save();
                return new ResponseMessage { ResponseCode = int.Parse(response.Response), Message = response.Message };
            }
        }

        private async Task<ResponseMessage> GenerateOtp(string phoneNumber, int userId)
        {
            string generateOtpCode = _uniqueIdentifier.GetUniqueCode(6);
            string otpMessage = $"Kindly use this OTP:{generateOtpCode} to complete the wallet creation/linking process on HealthInsured." +
                $"If you did not initiate this, kindly ignore";
            //Send User OTP SMS
            var smsresponse = await _smsService.SendSmsAsync(phoneNumber, otpMessage);
            if (smsresponse.Status)
            {
                var saveotp = new OtpValidation()
                {
                    OTP = generateOtpCode,
                    GeneratedDate = DateTimeOffset.Now,
                    ExpiredDate = DateTimeOffset.Now.AddMinutes(7),
                    ApplicationUserId = userId,
                    Status = true,
                };
                _repositoryWrapper.OtpValidation.Create(saveotp);
                await _repositoryWrapper.Save();
                return new ResponseMessage { ResponseCode = 00, Message = "Approved or Completed Successfully", Status = true };
            }
            return smsresponse;
        }
    }
}