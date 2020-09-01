using HealthBanc.Request.Tokenize;
using HealthBanc.Response;
using HealthBanc.Response.Tokenize;
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

        public TokenizationService(IWebHostEnvironment environment, IHttpClientFactory httpClientFactory,ILogger<TokenizationService> logger)
        {
            _environment = environment;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<ResponseMessage> ChargeCard(ChargeCard chargeCard)
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
                    var validResponse = new[] { "send_otp" , "send_pin", "success", "send_phone", "send_birthday", "open_url" };
                    if (!validResponse.Contains(chargeCardResponse.data.status)) return new ResponseMessage{Message = chargeCardResponse.data.url};
                    if (chargeCardResponse.data.status == "send_otp") return new ResponseMessage { Message = "Please enter your OTP code", Status = true, ResponseCode = 12 };
                    if (chargeCardResponse.data.status == "send_pin") return new ResponseMessage { Message = "" };
                    if (chargeCardResponse.data.status == "send_birthday") return new ResponseMessage { Message = "" };
                    if (chargeCardResponse.data.status == "send_phone") return new ResponseMessage { Message = "" };
                    if (chargeCardResponse.data.status == "success") return new ResponseMessage { Message = "" };
                    if (chargeCardResponse.data.status == "open_url") return new ResponseMessage { Message = "" };
                }
                _logger.LogCritical("Couldnt connect with payment service: ChargeCard Service:");
                return new ResponseMessage { Message = "Couldnt connect with payment service, please try again later", Status = false };
            }
            catch(Exception ex)
            {
                _logger.LogCritical("An error occurred while trying to charge cards: " + ex);
                return new ResponseMessage { Status = false };
            }
        }

        public async Task<ResponseMessage<ChargeCardResponse>> SendOtp( string otp)
        {
            try
            {
                var otpRequest = new SendOtp(otp, "12345");
                var httpClient = _httpClientFactory.CreateClient("Paystack");
                HttpContent content = new StringContent(JsonConvert.SerializeObject(otpRequest), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync("api/paystack/SubmitOTP", content);
                if (response.IsSuccessStatusCode)
                {
                    var otpResponse = new ChargeCardResponse();
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    otpResponse = JsonConvert.DeserializeObject<ChargeCardResponse>(apiResponse);
                    var validResponse = new[] { "send_otp", "send_pin", "success", "send_phone", "send_birthday", "open_url" };
                    if (!validResponse.Contains(otpResponse.data.status)) return new ResponseMessage<ChargeCardResponse>() { Message = otpResponse.data.url };
                }
                _logger.LogCritical("Couldnt connect with payment service: SendOtp Service:");
                return new ResponseMessage<ChargeCardResponse> {Message="Couldnt connect with payment service, please try again later", Status = false };
            }
            catch(Exception ex)
            {
                _logger.LogCritical("An error occurred while trying to send otp: " + ex);
                return new ResponseMessage<ChargeCardResponse> { Status = false };
            }
        }

        public async Task<ResponseMessage<ChargeCardResponse>> SendPin(string pin)
        {
            try
            {
                var pinRequest = new SendPin(pin, "123456");
                var httpClient = _httpClientFactory.CreateClient("Paystack");
                HttpContent content = new StringContent(JsonConvert.SerializeObject(pinRequest), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync("api/paystack/SubmitPIN", content);
                if (response.IsSuccessStatusCode)
                {
                    var pinResponse = new ChargeCardResponse();
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    pinResponse = JsonConvert.DeserializeObject<ChargeCardResponse>(apiResponse);
                    var validResponse = new[] { "send_otp", "send_pin", "success", "send_phone", "send_birthday", "open_url" };
                    if (!validResponse.Contains(pinResponse.data.status)) return new ResponseMessage<ChargeCardResponse>() { Message = pinResponse.data.url };
                }
                _logger.LogCritical("Couldnt connect with payment service: SendPin Service:");
                return new ResponseMessage<ChargeCardResponse> { Message = "Couldnt connect with payment service, please try again later", Status = false };
            }
            catch (Exception ex)
            {
                _logger.LogCritical("An error occurred while trying to send pin: " + ex);
                return new ResponseMessage<ChargeCardResponse> { Status = false };
            }
        }

        public async Task<ResponseMessage<ChargeCardResponse>> SubmitPhone(string phoneNumber)
        {
            try
            {
                var phoneRequest = new SubmitPhoneNumber(phoneNumber, "123456");
                var httpClient = _httpClientFactory.CreateClient("Paystack");
                HttpContent content = new StringContent(JsonConvert.SerializeObject(phoneRequest), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync("api/paystack/SubmitPhone", content);
                if (response.IsSuccessStatusCode)
                {
                    var phoneResponse = new ChargeCardResponse();
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    phoneResponse = JsonConvert.DeserializeObject<ChargeCardResponse>(apiResponse);
                    var validResponse = new[] { "send_otp", "send_pin", "success", "send_phone", "send_birthday", "open_url" };
                    if (!validResponse.Contains(phoneResponse.data.status)) return new ResponseMessage<ChargeCardResponse>() { Message = phoneResponse.data.url };
                }
                _logger.LogCritical("Couldnt connect with payment service: SubmitPhone Service:");
                return new ResponseMessage<ChargeCardResponse> { Message = "Couldnt connect with payment service, please try again later", Status = false };
            }
            catch (Exception ex)
            {
                _logger.LogCritical("An error occurred while trying to SubmitPhone: " + ex);
                return new ResponseMessage<ChargeCardResponse> { Status = false };
            }
        }

        public async Task<ResponseMessage<ChargeCardResponse>> SubmitBirthDay(DateTime date)
        {
            try
            {
                var birthRequest = new SubmitBirthday(date, "1234546");
                var httpClient = _httpClientFactory.CreateClient("Paystack");
                HttpContent content = new StringContent(JsonConvert.SerializeObject(birthRequest), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync("api/paystack/SubmitBirthDay", content);
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
