using AutoMapper;
using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Domain.Models;
using HealthBanc.DTO.TokenizationDTO;
using HealthBanc.Request.Tokenize;
using HealthBanc.Response;
using HealthBanc.Response.Tokenize;
using HealthBanc.ViewModels.Tokenization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
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

        public TokenizationService(IWebHostEnvironment environment, IHttpClientFactory httpClientFactory,ILogger<TokenizationService> logger, SendLogViaWhatApp logViaWhatApp,
            IAxaMansardUserProfileRepository axaMansardUser,IMapper mapper,IPaymentReferenceRepository paymentReference)
        {
            _environment = environment;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _logViaWhatApp = logViaWhatApp;
            _axaMansardUser = axaMansardUser;
            _mapper = mapper;
            _paymentReference = paymentReference;
        }

        public async Task<TokenizationResponse> ChargeCard(ChargeCard chargeCard,int id)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("Paystack");
                HttpContent content = new StringContent(JsonConvert.SerializeObject(chargeCard), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync("api/paystack/ChargeCard", content);
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
                        if (chargeCardResponse.data.status == "success") return new TokenizationResponse {Type = chargeCardResponse.data.authorization.card_type, LastDigit = chargeCardResponse.data.authorization.last4, 
                            AuthorizationCode = chargeCardResponse.data.authorization.authorization_code, Signature = chargeCardResponse.data.authorization.signature,
                            Message = "Card was tokenize successfully", Status = true,ResponseCode = 0};
                        if (chargeCardResponse.data.status == "open_url") return new TokenizationResponse { Message = "" };
                    }
                    return new TokenizationResponse { Message = chargeCardResponse.message,Status = false };
                }
                _logViaWhatApp.SendLog("Couldnt connect with payment service: ChargeCard Service:");
                _logger.LogCritical("Couldnt connect with payment service: ChargeCard Service:");
                return new TokenizationResponse { Message = "Couldnt connect with payment service, please try again later", Status = false };
            }
            catch(Exception ex)
            {
                _logViaWhatApp.SendLog(ex.ToString());
                _logger.LogCritical("An error occurred while trying to charge cards: " + ex);
                return new TokenizationResponse { Status = false };
            }
        }

        public async Task<TokenizationResponse> SendOtp( string otp,AxaMansardUserProfile user,string pin,string reference)
        {
            try
            {
                var otpRequest = new SendOtp(otp, reference);
                var httpClient = _httpClientFactory.CreateClient("Paystack");
                HttpContent content = new StringContent(JsonConvert.SerializeObject(otpRequest), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync("api/paystack/SubmitOTP", content);
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
                        if (otpResponse.data.status == "success") return new TokenizationResponse {Type=otpResponse.data.authorization.card_type , LastDigit = otpResponse.data.authorization.last4,
                            Signature = otpResponse.data.authorization.signature,AuthorizationCode =otpResponse.data.authorization.authorization_code,
                            Message = "Card was tokenize successfully", Status = true, ResponseCode = 0 };
                        if (otpResponse.data.status == "open_url") return new TokenizationResponse { Message = "" };
                    }
                    return new TokenizationResponse { Message = otpResponse.message, Status = false };
                   
                }
                _logger.LogCritical("Couldnt connect with payment service: SendOtp Service:");
                return new TokenizationResponse { Message="Couldnt connect with payment service, please try again later", Status = false };
            }
            catch(Exception ex)
            {
                _logger.LogCritical("An error occurred while trying to send otp: " + ex);
                return new TokenizationResponse { Status = false };
            }
        }

        private async Task<TokenizationResponse> SubmitPhone(string phoneNumber, AxaMansardUserProfile user,string pin,string reference)
        {
            try
            {
                var phoneRequest = new SubmitPhoneNumber(phoneNumber, reference);
                var httpClient = _httpClientFactory.CreateClient("Paystack");
                HttpContent content = new StringContent(JsonConvert.SerializeObject(phoneRequest), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync("api/paystack/SubmitPhone", content);
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
            catch (Exception ex)
            {
                _logViaWhatApp.SendLog("An error occurred while trying to SubmitPhone: " + ex.ToString());
                _logger.LogCritical("An error occurred while trying to SubmitPhone: " + ex);
                return new TokenizationResponse { Status = false, Message = "This on us. Can not tokenize card now, please try again later" };
            }
        }

        private async Task<TokenizationResponse> SubmitBirthDay(DateTime date, AxaMansardUserProfile user,string pin, string reference)
        {
            try
            {
                var birthRequest = new SubmitBirthday(date, reference);
                var httpClient = _httpClientFactory.CreateClient("Paystack");
                HttpContent content = new StringContent(JsonConvert.SerializeObject(birthRequest), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync("api/paystack/SubmitBirthDay", content);
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
            catch (Exception ex)
            {
                _logViaWhatApp.SendLog("An error occurred while trying to SubmitBirthDay: " + ex.ToString());
                _logger.LogCritical("An error occurred while trying to SubmitBirthDay: " + ex);
                return new TokenizationResponse { Status = false , Message = "This on us. Can not tokenize card now, please try again later" };
            }
        }

        public async Task InsertSubscription(AxaMansardBackgroundDTO userAxamansardProfile,TokenizationReference tokenization)
        {
            var requestId = Guid.NewGuid().ToString();
            var subscribePayment = _mapper.Map<SubscribePayment>(userAxamansardProfile);
            subscribePayment.Token = tokenization.Authorization_Code; subscribePayment.RequestId = requestId;
            subscribePayment.Fees = 0;

            var httpClient = _httpClientFactory.CreateClient("PaystackPayment");
            HttpContent content = new StringContent(JsonConvert.SerializeObject(subscribePayment), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("api/Subscription/InsertSubscription", content);
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
                    //return new ResponseMessage { Status = true, Message = subscribePaymentResponse.message };
                    await Task.CompletedTask;
                }
                //return new ResponseMessage { Status = false, Message = subscribePaymentResponse.message };
                await Task.CompletedTask;
            }
            //return new ResponseMessage { Status = false, Message = "Couldnt connect to payment service" };
            await Task.CompletedTask;
        }

        public async Task<ResponseMessage> UpdateSubscription(AxaMansardUserProfile userAxamansardProfile, TokenizationReference tokenization)
        {
            try
            {
                var subscribePayment = _mapper.Map<SubscribePayment>(userAxamansardProfile);
                subscribePayment.Token = tokenization.Authorization_Code; subscribePayment.RequestId = tokenization.TokenReference;
                var fee = subscribePayment.RepaymentAmount == 1000 ? 20 : 50;
                subscribePayment.Fees = fee;

                var httpClient = _httpClientFactory.CreateClient("PaystackPayment");
                HttpContent content = new StringContent(JsonConvert.SerializeObject(subscribePayment), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync("api/Subscription/UpdateSubscription", content);
                if (response.IsSuccessStatusCode)
                {
                    var updatePaymentResponse = new SubscribePaymentResponse();
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    updatePaymentResponse = JsonConvert.DeserializeObject<SubscribePaymentResponse>(apiResponse);
                    if (updatePaymentResponse.status == true)
                    {
                        return new ResponseMessage { Message = updatePaymentResponse.message, Status = true };
                    }
                    return new ResponseMessage { Message = updatePaymentResponse.message, Status = false };
                }
                return new ResponseMessage { Message = "Could not connect to paystack payment service" };
            }
            catch (Exception ex)
            {
                _logViaWhatApp.SendLog("An error occurred while trying to InsertSubscription: " + ex.ToString());
                _logger.LogCritical("An error occurred while trying to InsertSubscription: " + ex);
                return new ResponseMessage { Status = false, Message = "This on us. Can not insert subscription" };
            }
        }

        public async Task<ResponseMessage> CancelSubscription(AxaMansardUserProfile userAxamansardProfile)
        {
            try
            {
                var paymentReference = await _axaMansardUser.ActivePaymentReference(userAxamansardProfile.UserId);
                if(paymentReference == null)
                {
                    userAxamansardProfile.SubscriptionStatus = false;
                    _axaMansardUser.Update(userAxamansardProfile);
                    await _axaMansardUser.Save();
                }
                var subscribePayment = _mapper.Map<SubscribePayment>(userAxamansardProfile);
                subscribePayment.RequestId = paymentReference.RequestId; subscribePayment.Fees = 0;

                var httpClient = _httpClientFactory.CreateClient("PaystackPayment");
                HttpContent content = new StringContent(JsonConvert.SerializeObject(subscribePayment), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync("api/Subscription/CancelSubscription", content);
                if (response.IsSuccessStatusCode)
                {
                    var subscribePaymentResponse = new SubscribePaymentResponse();
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    subscribePaymentResponse = JsonConvert.DeserializeObject<SubscribePaymentResponse>(apiResponse);
                    if (subscribePaymentResponse.status == true)
                    {
                        userAxamansardProfile.SubscriptionStatus = false;
                        _axaMansardUser.Update(userAxamansardProfile);
                        await _axaMansardUser.Save();

                        return new ResponseMessage { Message = subscribePaymentResponse.message, Status = true };
                    }
                    return new ResponseMessage { Message = subscribePaymentResponse.message, Status = false };
                }
                return new ResponseMessage { Message = "Could not connect to paystack payment service" };
            }
            catch (Exception ex)
            {
                _logViaWhatApp.SendLog("An error occurred while trying to InsertSubscription: " + ex.ToString());
                _logger.LogCritical("An error occurred while trying to InsertSubscription: " + ex);
                return new ResponseMessage { Status = false, Message = "Can not cancel subscrption now, please try again later" };
            }

        }

        public async Task<ResponseMessage<ChargeCardResponse>> ValidateCharge (string reference)
        {
            try
            {
                var validateChargeRequest = new ValidateCharge(reference);
                var httpClient = _httpClientFactory.CreateClient("Paystack");
                HttpContent content = new StringContent(JsonConvert.SerializeObject(validateChargeRequest), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync("api/paystack/ValidateCharge", content);
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
