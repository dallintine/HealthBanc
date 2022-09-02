using Application.API_RequestModel.Wallet;
using Application.API_ResponseModel.Wallet;
using Application.DTO;
using Application.Helpers;
using Application.Interfaces;
using DataAccess;
using Domain.Enums;
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
    public class WalletPaymentService
    {
        private readonly WalletConnect _walletConnect;
        private readonly ILogger<WalletPaymentService> _logger;
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly IWalletEncryptionsAndDecryption _encryptionsAndDecryption;
        private readonly WalletSettings _walletSettings;

        public WalletPaymentService(WalletConnect walletConnect, ILogger<WalletPaymentService> logger, IRepositoryWrapper repositoryWrapper,
            IWalletEncryptionsAndDecryption encryptionsAndDecryption, IOptions<WalletSettings> WalletSettings)
        {
            _walletConnect = walletConnect;
            _logger = logger;
            _repositoryWrapper = repositoryWrapper;
            _encryptionsAndDecryption = encryptionsAndDecryption;
            _walletSettings = WalletSettings.Value;
        }

        public async Task<ResponseMessage> WalletToSterling(int userId, decimal amount, string mobileNumber, string channel)
        {
            var wallettransfer = new WalletToAccount
            {
                CURRENCYCODE = "NGN",
                ChannelID = int.Parse(_walletSettings.ChannelId),
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
            var transferResponse = await _walletConnect.WalletToSterlingFT(encryptData);
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
    }
}
