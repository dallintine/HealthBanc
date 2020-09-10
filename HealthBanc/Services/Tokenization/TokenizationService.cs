using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Domain.Models;
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

        public TokenizationService(IWebHostEnvironment environment, IHttpClientFactory httpClientFactory,ILogger<TokenizationService> logger, SendLogViaWhatApp logViaWhatApp,
            IAxaMansardUserProfileRepository axaMansardUser)
        {
            _environment = environment;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _logViaWhatApp = logViaWhatApp;
            _axaMansardUser = axaMansardUser;
        }

        public async Task<ResponseMessage> ChargeCard(ChargeCard chargeCard,int id)
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
                        if (!validResponse.Contains(chargeCardResponse.data.status)) return new ResponseMessage { Message = chargeCardResponse.data.url };
                        if (chargeCardResponse.data.status == "send_otp")
                        {
                            var otpViewModel = new SetOtpViewModel(null, chargeCard.pin, chargeCard.reference);
                            return new ResponseMessage {Data = otpViewModel, Message = "Please enter your OTP code", Status = true, ResponseCode = 12, };
                        }
                        if (chargeCardResponse.data.status == "send_pin")
                        {
                            var user = await _axaMansardUser.GetByAdminIdAsync(id);
                            if (user != null)
                            {
                                var result = await SendPin(chargeCard.pin,user,chargeCard.reference);
                                return result;
                            }                            
                        }
                        if (chargeCardResponse.data.status == "send_birthday")
                        {
                            var user = await _axaMansardUser.GetByAdminIdAsync(id);
                            if (user != null)
                            {
                                var result = await SubmitBirthDay(user.DateOfBirth,user,chargeCard.pin,chargeCard.reference);
                                return result;
                            }
                        }
                        if (chargeCardResponse.data.status == "send_phone") return new ResponseMessage { Message = "" };
                        if (chargeCardResponse.data.status == "success") return new ResponseMessage { AuthorizationCode = chargeCardResponse.data.authorization_code, Message = "Card was tokenize successfully", Status = true,ResponseCode = 0};
                        if (chargeCardResponse.data.status == "open_url") return new ResponseMessage { Message = "" };
                    }
                    return new ResponseMessage { Message = chargeCardResponse.message,Status = false };
                }
                _logViaWhatApp.SendLog("Couldnt connect with payment service: ChargeCard Service:");
                _logger.LogCritical("Couldnt connect with payment service: ChargeCard Service:");
                return new ResponseMessage { Message = "Couldnt connect with payment service, please try again later", Status = false };
            }
            catch(Exception ex)
            {
                _logViaWhatApp.SendLog(ex.ToString());
                _logger.LogCritical("An error occurred while trying to charge cards: " + ex);
                return new ResponseMessage { Status = false };
            }
        }

        public async Task<ResponseMessage> SendOtp( string otp,AxaMansardUserProfile user,string pin,string reference)
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
                        if (!validResponse.Contains(otpResponse.data.status)) return new ResponseMessage { Message = otpResponse.data.url };
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
                            var result = await SendPin(pin, user,reference);
                        }
                        if (otpResponse.data.status == "success") return new ResponseMessage {AuthorizationCode =otpResponse.data.authorization_code, Message = "Card was tokenize successfully", Status = true, ResponseCode = 0 };
                        if (otpResponse.data.status == "open_url") return new ResponseMessage { Message = "" };
                    }
                    return new ResponseMessage { Message = otpResponse.message, Status = false };
                   
                }
                _logger.LogCritical("Couldnt connect with payment service: SendOtp Service:");
                return new ResponseMessage{Message="Couldnt connect with payment service, please try again later", Status = false };
            }
            catch(Exception ex)
            {
                _logger.LogCritical("An error occurred while trying to send otp: " + ex);
                return new ResponseMessage{ Status = false };
            }
        }

        private async Task<ResponseMessage> SendPin(string pin,AxaMansardUserProfile user, string reference)
        {
            try
            {
                var pinRequest = new SendPin(pin, reference);
                var httpClient = _httpClientFactory.CreateClient("Paystack");
                HttpContent content = new StringContent(JsonConvert.SerializeObject(pinRequest), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync("api/paystack/SubmitPIN", content);
                if (response.IsSuccessStatusCode)
                {
                    var pinResponse = new ChargeCardResponse();
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    pinResponse = JsonConvert.DeserializeObject<ChargeCardResponse>(apiResponse);
                    if(pinResponse.status == true)
                    {
                        var validResponse = new[] { "send_otp", "success", "send_phone", "send_birthday", "open_url" };
                        if (!validResponse.Contains(pinResponse.data.status)) return new ResponseMessage() { Message = pinResponse.data.url };
                        if (pinResponse.data.status == "send_otp") return new ResponseMessage { Message = "Please enter your OTP code", Status = true, ResponseCode = 12 };                       
                        if (pinResponse.data.status == "send_birthday")
                        {
                                var result = await SubmitBirthDay(user.DateOfBirth,user,pin,reference);
                        }
                        if (pinResponse.data.status == "send_phone")
                        {
                            var result = await SubmitPhone(user.PhoneNumber,user,pin,reference);
                        }
                        if (pinResponse.data.status == "success") return new ResponseMessage { Message = "Card was tokenize successfully", Status = true, ResponseCode = 0 };
                        if (pinResponse.data.status == "open_url") return new ResponseMessage { Message = "" };
                    }
                    return new ResponseMessage() { Message = pinResponse.message };
                }
                _logger.LogCritical("Couldnt connect with payment service: SendPin Service:");
                return new ResponseMessage{ Message = "Couldnt connect with payment service, please try again later", Status = false };
            }
            catch (Exception ex)
            {
                _logViaWhatApp.SendLog("An error occurred while trying to send pin: " + ex.ToString());
                _logger.LogCritical("An error occurred while trying to send pin: " + ex);
                return new ResponseMessage{ Status = false, Message="This on us. Can not tokenize card now, please try again later" };
            }
        }

        private async Task<ResponseMessage> SubmitPhone(string phoneNumber, AxaMansardUserProfile user,string pin,string reference)
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
                        if (!validResponse.Contains(phoneResponse.data.status)) return new ResponseMessage() { Message = phoneResponse.data.url };
                        if (phoneResponse.data.status == "send_otp") return new ResponseMessage { Message = "Please enter your OTP code", Status = true, ResponseCode = 12 };
                        if (phoneResponse.data.status == "send_birthday")
                        {
                            var result = await SubmitBirthDay(user.DateOfBirth,user,pin,reference);
                        }
                        if (phoneResponse.data.status == "send_pin")
                        {
                            var result = await SendPin(pin, user,reference);
                        }
                        if (phoneResponse.data.status == "success") return new ResponseMessage { Message = "Card was tokenize successfully", Status = true, ResponseCode = 0 };
                        if (phoneResponse.data.status == "open_url") return new ResponseMessage { Message = "" };
                    }
                    return new ResponseMessage { Message = phoneResponse.message, Status = false };
                }
                _logViaWhatApp.SendLog("Couldnt connect with payment service: SubmitPhone Service:");
                _logger.LogCritical("Couldnt connect with payment service: SubmitPhone Service:");
                return new ResponseMessage { Message = "Couldnt connect with payment service, please try again later", Status = false };
            }
            catch (Exception ex)
            {
                _logViaWhatApp.SendLog("An error occurred while trying to SubmitPhone: " + ex.ToString());
                _logger.LogCritical("An error occurred while trying to SubmitPhone: " + ex);
                return new ResponseMessage { Status = false, Message = "This on us. Can not tokenize card now, please try again later" };
            }
        }

        private async Task<ResponseMessage> SubmitBirthDay(DateTime date, AxaMansardUserProfile user,string pin, string reference)
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
                        if (!validResponse.Contains(birthdayResponse.data.status)) return new ResponseMessage { Message = birthdayResponse.data.url };
                        if (birthdayResponse.data.status == "send_otp") return new ResponseMessage { Message = "Please enter your OTP code", Status = true, ResponseCode = 12 };
                        if (birthdayResponse.data.status == "send_phone")
                        {
                            var result = await SubmitPhone(user.PhoneNumber, user, pin,reference);
                        }
                        if (birthdayResponse.data.status == "send_pin")
                        {
                            var result = await SendPin(pin, user,reference);
                        }
                        if (birthdayResponse.data.status == "success") return new ResponseMessage { Message = "Card was tokenize successfully", Status = true, ResponseCode = 0 };
                        if (birthdayResponse.data.status == "open_url") return new ResponseMessage { Message = "" };
                    }
                    return new ResponseMessage { Status = false, Message = birthdayResponse.message };                 
                }
                _logViaWhatApp.SendLog("Couldnt connect with payment service: SubmitBirthDay Service:");
                _logger.LogCritical("Couldnt connect with payment service: SubmitBirthDay Service:");
                return new ResponseMessage{ Message = "Couldnt connect with payment service, please try again later", Status = false };
            }
            catch (Exception ex)
            {
                _logViaWhatApp.SendLog("An error occurred while trying to SubmitBirthDay: " + ex.ToString());
                _logger.LogCritical("An error occurred while trying to SubmitBirthDay: " + ex);
                return new ResponseMessage { Status = false , Message = "This on us. Can not tokenize card now, please try again later" };
            }
        }

        public async Task<ResponseMessage> InsertSubscription(SubscribePayment subscribePayment)
        {
            try
            {
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
                        return new ResponseMessage { Message = subscribePaymentResponse.message, Status = true };
                    }
                    return new ResponseMessage { Message = subscribePaymentResponse.message, Status = false };
                }
                return new ResponseMessage { Message = "Could not connect to paystack payment service" };
            }
            catch(Exception ex)
            {
                _logViaWhatApp.SendLog("An error occurred while trying to InsertSubscription: " + ex.ToString());
                _logger.LogCritical("An error occurred while trying to InsertSubscription: " + ex);
                return new ResponseMessage { Status = false, Message = "This on us. Can not insert subscription" };
            }            
        }

        public async Task<ResponseMessage> UpdateSubscription(SubscribePayment subscribePayment)
        {
            try
            {
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
