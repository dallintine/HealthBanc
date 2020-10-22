using AutoMapper;
using Hangfire;
using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Domain.Models;
using HealthBanc.DTO.TokenizationDTO;
using HealthBanc.Helpers;
using HealthBanc.Helpers.ThirdPartyAPI;
using HealthBanc.Request.Tokenize;
using HealthBanc.Response;
using HealthBanc.Response.Tokenize;
using HealthBanc.Services.AuditAndReport.AuditLog;
using HealthBanc.ViewModels;
using HealthBanc.ViewModels.Tokenization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HealthBanc.Services.Tokenization
{
    public class TokenizationService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TokenizationService> _logger;
        private readonly SendLogViaWhatApp _logViaWhatApp;
        private readonly IAxaMansardUserProfileRepository _axaMansardUser;
        private readonly IMapper _mapper;
        private readonly IPaymentReferenceRepository _paymentReference;
        private readonly AuditLogService _auditLogServices;
        private AppEndpoint Options { get; }
        private Paystack PaystackOptions { get; set; }

        public TokenizationService(IWebHostEnvironment environment, IHttpClientFactory httpClientFactory,ILogger<TokenizationService> logger, SendLogViaWhatApp logViaWhatApp,
            IAxaMansardUserProfileRepository axaMansardUser,IMapper mapper,IPaymentReferenceRepository paymentReference, IOptions<AppEndpoint> optionAccessor,
            AuditLogService auditLogServices, IOptions<Paystack> paystackOptions)
        {
            Options = optionAccessor.Value;
            PaystackOptions = paystackOptions.Value;
            _environment = environment;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _logViaWhatApp = logViaWhatApp;
            _axaMansardUser = axaMansardUser;
            _mapper = mapper;
            _paymentReference = paymentReference;
            _auditLogServices = auditLogServices;
        }

        public async Task<TokenizationResponse> ChargeCard(ChargeCard chargeCard,int id,string ipAddress,string device)
        {
            var httpClient = _httpClientFactory.CreateClient("Paystack");
            HttpContent content = new StringContent(JsonConvert.SerializeObject(chargeCard), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(Options.APIUri.PayStackTokenisationChargeCard, content);
            if (response.IsSuccessStatusCode)
            {
                var chargeCardResponse = new ChargeCardResponse();
                string apiResponse = await response.Content.ReadAsStringAsync();
                chargeCardResponse = JsonConvert.DeserializeObject<ChargeCardResponse>(apiResponse);
                if(chargeCardResponse.status is true)
                {
                    var validResponse = new[] { "send_otp", "send_pin", "success", "send_phone", "send_birthday", "open_url" };
                    if (!validResponse.Contains(chargeCardResponse.data.status)) return new TokenizationResponse { Message = chargeCardResponse.data.url };
                    if (chargeCardResponse.data.status == "send_otp")
                    {
                        var otpViewModel = new SetOtpViewModel(null, chargeCard.pin, chargeCard.reference);
                        return new TokenizationResponse { Data = otpViewModel, Message = "Please enter your OTP code", Status = true, ResponseCode = 12, };
                    }
                    if (chargeCardResponse.data.status == "send_pin")
                    {
                        return new TokenizationResponse { Status = false, Message = "Please enter a valid pin number" };
                    }
                    if (chargeCardResponse.data.status == "send_birthday")
                    {
                        var user = await _axaMansardUser.GetByAdminIdAsync(id);
                        if (user != null)
                        {
                            var result = await SubmitBirthDay(user.DateOfBirth,user,chargeCard.pin,chargeCard.reference);
                            return new TokenizationResponse { Status = result.Status, Message = result.Message, ResponseCode = result.ResponseCode };
                        }
                    }
                    if (chargeCardResponse.data.status == "send_phone") return new TokenizationResponse { Message = "" };
                    if (chargeCardResponse.data.status == "success")
                    {
                        var auditViewModel = new AuditLogViewModel(id, null, null, "Successfully Card Tokenization", $"Tokenization reference is {chargeCard.reference}");
                        BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel,ipAddress,device));

                        return new TokenizationResponse
                        {
                            Type = chargeCardResponse.data.authorization.card_type,
                            LastDigit = chargeCardResponse.data.authorization.last4,
                            AuthorizationCode = chargeCardResponse.data.authorization.authorization_code,
                            Signature = chargeCardResponse.data.authorization.signature,
                            Message = "Card was tokenize successfully",
                            Status = true,
                            ResponseCode = 0
                        };
                    } 
                    if (chargeCardResponse.data.status == "open_url") return new TokenizationResponse { Message = "" };
                }
                var message = chargeCardResponse.data.message != null ? chargeCardResponse.data.message : "";
                return new TokenizationResponse { Message = chargeCardResponse.message+", "+message,Status = false };
            }
            _logViaWhatApp.SendLog("Couldnt connect with payment service: ChargeCard Service:");
            _logger.LogCritical("Couldnt connect with payment service: ChargeCard Service:");
            return new TokenizationResponse { Message = "Couldnt connect with payment service, please try again later", Status = false };
        }

        public async Task<TokenizationResponse> SendOtp( string otp,AxaMansardUserProfile user,string pin,string reference, string ipAddress, string device)
        {
            var otpRequest = new SendOtp(otp, reference);
            var httpClient = _httpClientFactory.CreateClient("Paystack");
            HttpContent content = new StringContent(JsonConvert.SerializeObject(otpRequest), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(Options.APIUri.PayStackTokenisationSendOtp, content);
            if (response.IsSuccessStatusCode)
            {
                var otpResponse = new ChargeCardResponse();
                string apiResponse = await response.Content.ReadAsStringAsync();
                otpResponse = JsonConvert.DeserializeObject<ChargeCardResponse>(apiResponse);
                if(otpResponse.status == true)
                {
                    var validResponse = new[] { "send_otp", "send_pin", "success", "send_phone", "send_birthday", "open_url" };
                    if (!validResponse.Contains(otpResponse.data.status)) return new TokenizationResponse { Message = otpResponse.data.url };
                    if (otpResponse.data.status == "send_birthday")
                    {
                        var result = await SubmitBirthDay(user.DateOfBirth, user, pin,reference);
                    }
                    if (otpResponse.data.status == "send_phone")
                    {
                        var result = await SubmitPhone(user.PhoneNumber, user, pin,reference);
                    }
                    if (otpResponse.data.status == "send_pin")
                    {
                        return new TokenizationResponse { Status = false, Message = "Please enter a valid pin number" };
                    }
                    if (otpResponse.data.status == "success") 
                    {
                        var auditViewModel = new AuditLogViewModel(user.UserId, null, null, "Successfully Card Tokenization", $"Tokenization reference is {reference}");
                        BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel,ipAddress,device));

                        return new TokenizationResponse
                        {
                            Type = otpResponse.data.authorization.card_type,
                            LastDigit = otpResponse.data.authorization.last4,
                            Signature = otpResponse.data.authorization.signature,
                            AuthorizationCode = otpResponse.data.authorization.authorization_code,
                            Message = "Card was tokenize successfully",
                            Status = true,
                            ResponseCode = 0
                        };
                    }
                    if (otpResponse.data.status == "open_url") return new TokenizationResponse { Message = "" };
                }
                return new TokenizationResponse { Message = otpResponse.message, Status = false };
                   
            }
            _logger.LogCritical("Couldnt connect with payment service: SendOtp Service:");
            return new TokenizationResponse { Message="Couldnt connect with payment service, please try again later", Status = false };
        }

        private async Task<TokenizationResponse> SubmitPhone(string phoneNumber, AxaMansardUserProfile user,string pin,string reference)
        {
            var phoneRequest = new SubmitPhoneNumber(phoneNumber, reference);
            var httpClient = _httpClientFactory.CreateClient("Paystack");
            HttpContent content = new StringContent(JsonConvert.SerializeObject(phoneRequest), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(Options.APIUri.PayStackTokenisationSubmitPhone, content);
            if (response.IsSuccessStatusCode)
            {
                var phoneResponse = new ChargeCardResponse();
                string apiResponse = await response.Content.ReadAsStringAsync();
                phoneResponse = JsonConvert.DeserializeObject<ChargeCardResponse>(apiResponse);
                if(phoneResponse.status is true)
                {
                    var validResponse = new[] { "send_otp", "send_pin", "success", "send_phone", "send_birthday", "open_url" };
                    if (!validResponse.Contains(phoneResponse.data.status)) return new TokenizationResponse() { Message = phoneResponse.data.url };
                    if (phoneResponse.data.status == "send_otp") return new TokenizationResponse { Message = "Please enter your OTP code", Status = true, ResponseCode = 12 };
                    if (phoneResponse.data.status == "send_birthday")
                    {
                        var result = await SubmitBirthDay(user.DateOfBirth,user,pin,reference);
                    }
                    if (phoneResponse.data.status == "send_pin")
                    {
                        return new TokenizationResponse { Status = false, Message = "Please enter a valid pin number" };
                    }
                    if (phoneResponse.data.status == "success") return new TokenizationResponse {Signature = phoneResponse.data.authorization.signature,
                        Type = phoneResponse.data.authorization.card_type,LastDigit = phoneResponse.data.authorization.last4,
                        AuthorizationCode = phoneResponse.data.authorization.authorization_code,Message = "Card was tokenize successfully", Status = true, ResponseCode = 0 };
                    if (phoneResponse.data.status == "open_url") return new TokenizationResponse { Message = "" };
                }
                return new TokenizationResponse { Message = phoneResponse.message, Status = false };
            }
            _logViaWhatApp.SendLog("Couldnt connect with payment service: SubmitPhone Service:");
            _logger.LogCritical("Couldnt connect with payment service: SubmitPhone Service:");
            return new TokenizationResponse { Message = "Couldnt connect with payment service, please try again later", Status = false };
        }

        private async Task<TokenizationResponse> SubmitBirthDay(DateTime date, AxaMansardUserProfile user,string pin, string reference)
        {
            var birthRequest = new SubmitBirthday(date, reference);
            var httpClient = _httpClientFactory.CreateClient("Paystack");
            HttpContent content = new StringContent(JsonConvert.SerializeObject(birthRequest), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(Options.APIUri.PayStackTokenisationaSubmitBirthDay, content);
            if (response.IsSuccessStatusCode)
            {
                var birthdayResponse = new ChargeCardResponse();
                string apiResponse = await response.Content.ReadAsStringAsync();
                birthdayResponse = JsonConvert.DeserializeObject<ChargeCardResponse>(apiResponse);
                if(birthdayResponse.status == true)
                {
                    var validResponse = new[] { "send_otp", "send_pin", "success", "send_phone", "send_birthday", "open_url" };
                    if (!validResponse.Contains(birthdayResponse.data.status)) return new TokenizationResponse { Message = birthdayResponse.data.url };
                    if (birthdayResponse.data.status == "send_otp") return new TokenizationResponse { Message = "Please enter your OTP code", Status = true, ResponseCode = 12 };
                    if (birthdayResponse.data.status == "send_phone")
                    {
                        var result = await SubmitPhone(user.PhoneNumber, user, pin,reference);
                    }
                    if (birthdayResponse.data.status == "send_pin")
                    {
                        return new TokenizationResponse { Status = false, Message = "Please enter a valid pin number" };
                    }
                    if (birthdayResponse.data.status == "success") return new TokenizationResponse {Signature = birthdayResponse.data.authorization.signature,
                        Type = birthdayResponse.data.authorization.card_type, LastDigit = birthdayResponse.data.authorization.last4,
                        AuthorizationCode = birthdayResponse.data.authorization.authorization_code, Message = "Card was tokenize successfully", Status = true, ResponseCode = 0 };
                    if (birthdayResponse.data.status == "open_url") return new TokenizationResponse { Message = "" };
                }
                return new TokenizationResponse { Status = false, Message = birthdayResponse.message };                 
            }
            _logViaWhatApp.SendLog("Couldnt connect with payment service: SubmitBirthDay Service:");
            _logger.LogCritical("Couldnt connect with payment service: SubmitBirthDay Service:");
            return new TokenizationResponse { Message = "Couldnt connect with payment service, please try again later", Status = false };
        }

        public async Task InsertSubscription(AxaMansardBackgroundDTO userAxamansardProfile,TokenizationReference tokenization,string ipAddress, string device)
        {
            var requestId = Guid.NewGuid().ToString();
            var subscribePayment = _mapper.Map<SubscribePayment>(userAxamansardProfile);
            subscribePayment.Token = tokenization.Authorization_Code; subscribePayment.RequestId = requestId;
            subscribePayment.NextRepaymentDate = DateTime.Now.AddMonths(1);
            subscribePayment.Channel = PaystackOptions.Config.Channel; subscribePayment.TokenType = PaystackOptions.Config.TokenType;

            var httpClient = _httpClientFactory.CreateClient("PaystackPayment");
            HttpContent content = new StringContent(JsonConvert.SerializeObject(subscribePayment), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(Options.APIUri.PaystackPaymentInsertSubscription, content);
            if (response.IsSuccessStatusCode)
            {
                var subscribePaymentResponse = new SubscribePaymentResponse();
                string apiResponse = await response.Content.ReadAsStringAsync();
                subscribePaymentResponse = JsonConvert.DeserializeObject<SubscribePaymentResponse>(apiResponse);
                if (subscribePaymentResponse.status == true)
                {
                    var payment = new PaymentReference()
                    {
                        Date = DateTime.Now,
                        AxaMansardUserProfileId = userAxamansardProfile.Id,
                        UserId = userAxamansardProfile.UserId,
                        Amount = userAxamansardProfile.Premium,
                        RequestId = requestId,
                        Active = true,
                    };
                    _paymentReference.Create(payment);
                    await _paymentReference.Save();

                    var auditViewModel = new AuditLogViewModel(userAxamansardProfile.UserId, subscribePayment.RequestId, "Inactive subscription status", "Subscription Status Changed",
                           "Active subscription status");
                    BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel,ipAddress,device));
                    await Task.CompletedTask;
                }
                await Task.CompletedTask;
            }
            await Task.CompletedTask;
        }

        public async Task UpdateSubscription(AxaMansardBackgroundDTO userAxamansardProfile, string authorization_Code, string requestId,string ipAddress,
            string device)
        {
            var subscribePayment = _mapper.Map<SubscribePayment>(userAxamansardProfile);
            subscribePayment.Token = authorization_Code; subscribePayment.RequestId = requestId;
            subscribePayment.Channel = PaystackOptions.Config.Channel; subscribePayment.TokenType = PaystackOptions.Config.TokenType;

            var httpClient = _httpClientFactory.CreateClient("PaystackPayment");
            HttpContent content = new StringContent(JsonConvert.SerializeObject(subscribePayment), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(Options.APIUri.PaystackPaymentUpdateSubscription, content);
            if (response.IsSuccessStatusCode)
            {
                var updatePaymentResponse = new SubscribePaymentResponse();
                string apiResponse = await response.Content.ReadAsStringAsync();
                updatePaymentResponse = JsonConvert.DeserializeObject<SubscribePaymentResponse>(apiResponse);
                if (updatePaymentResponse.status == true)
                {
                    var auditViewModel = new AuditLogViewModel(userAxamansardProfile.UserId, subscribePayment.RequestId, "", "Changed primary card",
                        "");
                    BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel,ipAddress,device));
                    await Task.CompletedTask;
                }
                await Task.CompletedTask;
            }
            await Task.CompletedTask;
        }

        public async Task<ResponseMessage> CancelSubscription(AxaMansardUserProfile userAxamansardProfile,string ipAddress, string device)
        {
            var paymentReference = await _axaMansardUser.ActivePaymentReference(userAxamansardProfile.UserId);
            if(paymentReference == null)
            {
                userAxamansardProfile.SubscriptionStatus = false;
                _axaMansardUser.Update(userAxamansardProfile);
                await _axaMansardUser.Save();

                var auditViewModel = new AuditLogViewModel(userAxamansardProfile.UserId, null, "Active subscription status", "Subscription Status Changed",
                        "Inactive subscription status");
                BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel,ipAddress,device));

                return new ResponseMessage { Message = "Subscription was cancelled successfully", Status = true };
            }
            var subscribePayment = _mapper.Map<SubscribePayment>(userAxamansardProfile);
            subscribePayment.RequestId = paymentReference.RequestId; subscribePayment.Fees = 0;
            subscribePayment.Channel = PaystackOptions.Config.Channel; subscribePayment.TokenType = PaystackOptions.Config.TokenType;

            var httpClient = _httpClientFactory.CreateClient("PaystackPayment");
            HttpContent content = new StringContent(JsonConvert.SerializeObject(subscribePayment), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(Options.APIUri.PaystackPaymentCancelSubscription, content);
            if (response.IsSuccessStatusCode)
            {
                var subscribePaymentResponse = new SubscribePaymentResponse();
                string apiResponse = await response.Content.ReadAsStringAsync();
                subscribePaymentResponse = JsonConvert.DeserializeObject<SubscribePaymentResponse>(apiResponse);
                if (subscribePaymentResponse.status == true)
                {
                    paymentReference.Active = false;
                    _paymentReference.Update(paymentReference);
                        
                    userAxamansardProfile.SubscriptionStatus = false;
                    _axaMansardUser.Update(userAxamansardProfile);
                    await _axaMansardUser.Save();

                    var auditViewModel = new AuditLogViewModel(userAxamansardProfile.UserId, subscribePayment.RequestId, "Active subscription status", "Subscription Status Changed",
                        "Inactive subscription status");
                    BackgroundJob.Enqueue(() =>  _auditLogServices.UserCreateAuditLog(auditViewModel,ipAddress,device));

                    return new ResponseMessage { Message = subscribePaymentResponse.message, Status = true };
                }
                return new ResponseMessage { Message = subscribePaymentResponse.message, Status = false };
            }
            return new ResponseMessage { Message = "Could not connect to paystack payment service" };
        }

        public async Task<ResponseMessage<ChargeCardResponse>> ValidateCharge (string reference)
        {
            try
            {
                var validateChargeRequest = new ValidateCharge(reference);
                var httpClient = _httpClientFactory.CreateClient("Paystack");
                HttpContent content = new StringContent(JsonConvert.SerializeObject(validateChargeRequest), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(Options.APIUri.PayStackTokenisationValidateCharge, content);
                if (response.IsSuccessStatusCode)
                {
                    var birthdayResponse = new ChargeCardResponse();
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    birthdayResponse = JsonConvert.DeserializeObject<ChargeCardResponse>(apiResponse);
                    var validResponse = new[] { "send_otp", "send_pin", "success", "send_phone", "send_birthday", "open_url" };
                    if (!validResponse.Contains(birthdayResponse.data.status)) return new ResponseMessage<ChargeCardResponse>() { Message = birthdayResponse.data.url };
                }
                _logger.LogCritical("Couldnt connect with payment service: SubmitBirthDay Service:");
                return new ResponseMessage<ChargeCardResponse> { Message = "Couldnt connect with payment service, please try again later", Status = false };
            }
            catch (Exception ex)
            {
                _logger.LogCritical("An error occurred while trying to SubmitBirthDay: " + ex);
                return new ResponseMessage<ChargeCardResponse> { Status = false };
            }
        }
    }
}
