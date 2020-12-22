using Application.API_RequestModel.Paystack;
using Application.API_ResponseModel.Paystack;
using Application.Helpers.ThirdPartyAPI;
using Application.ViewModels.Paystack;
using DataAccess.HealthInsured_AxaMansard.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Paystack
{
    public class PaystackService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAxaMansardUserProfileRepository _axaMansardUser;
        private readonly ILogger<PaystackService> _logger;

        private Helpers.Paystack Options { get; }

        public PaystackService(IHttpClientFactory httpClientFactory,IAxaMansardUserProfileRepository axaMansardUser, IOptions<Helpers.Paystack> paystackAccessor,
            ILogger<PaystackService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _axaMansardUser = axaMansardUser;
            _logger = logger;
            Options = paystackAccessor.Value;
        }
        
        public async Task<TokenizationResponse> ChargeCard(ChargeCard chargeCard, int id)
        {
            // Call Paystack client
            var httpClient = _httpClientFactory.CreateClient("Paystack");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", $"{Options.SecretKey}");
            HttpContent content = new StringContent(JsonConvert.SerializeObject(chargeCard), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync($"{Options.PayStackChargeCard}", content);

            string apiResponse = await response.Content.ReadAsStringAsync();
            var chargeCardResponse = JsonConvert.DeserializeObject<ChargeCardResponse>(apiResponse);
            if (response.IsSuccessStatusCode)
            {     
                // Check if the response from paystack has a status of true
                if (chargeCardResponse.status is true)
                {
                    // call ProcessSuccessOrFailedResult function to process failed,timeout or succees response
                    var processSuccessOrFailedResult = ProcessSuccessOrFailedDataStatus(chargeCardResponse.data.authorization, chargeCardResponse.data.status,
                        chargeCardResponse.message);

                    // check if status is true
                    if(processSuccessOrFailedResult.Status)
                    {
                        return processSuccessOrFailedResult;
                    }
                    // check if status is failed or timeout
                    else if(!processSuccessOrFailedResult.Status && processSuccessOrFailedResult.ResponseCode!= 13)
                    {
                        return processSuccessOrFailedResult;
                    }
                    // check if sucess is neither failes, success or timeout means response code is 13
                    else
                    {
                        var user = await _axaMansardUser.GetByUserIdAsync(id);

                        // call ProcessValidDataStatus fucntion to process other valid response {send_otp,submit_birthday,send_phonenumber}
                        var processStatusResponse = await ProcessValidDataStatus(chargeCardResponse.data.status, chargeCard.reference, user.PhoneNumber, user.DateOfBirth);
                        return processStatusResponse;
                    }  
                }
                var message = chargeCardResponse.data.message != null ? chargeCardResponse.data.message : "";
                return new TokenizationResponse { Message = chargeCardResponse.message + ", " + message, Status = false };
            }
            var errorMessage = chargeCardResponse.data.message != null ? chargeCardResponse.data.message : "";
            return new TokenizationResponse { Message = chargeCardResponse.message + ", " + errorMessage, Status = false };
        }

        public async Task<TokenizationResponse> SendOtp(string otp, string reference,string phoneNumber, DateTime dateOfBirth)
        {
            var otpRequest = new SendOtp(otp, reference);
            // call paystack client
            var httpClient = _httpClientFactory.CreateClient("Paystack");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", $"{Options.SecretKey}");
            HttpContent content = new StringContent(JsonConvert.SerializeObject(otpRequest), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync($"{Options.PayStackSendOtp}", content);

            string apiResponse = await response.Content.ReadAsStringAsync();
            var otpResponse = JsonConvert.DeserializeObject<ChargeCardResponse>(apiResponse);
            if (response.IsSuccessStatusCode)
            {               
                if (otpResponse.status == true)
                {
                    // call ProcessSuccessOrFailedResult function to process failed,timeout or succees response
                    var processSuccessOrFailedResult = ProcessSuccessOrFailedDataStatus(otpResponse.data.authorization, otpResponse.data.status,
                        otpResponse.message);

                    // check if status is true
                    if (processSuccessOrFailedResult.Status)
                    {
                        return processSuccessOrFailedResult;
                    }
                    // check if status is failed or timeout
                    else if (!processSuccessOrFailedResult.Status && processSuccessOrFailedResult.ResponseCode != 13)
                    {
                        return processSuccessOrFailedResult;
                    }
                    // check if sucess is neither failes, success or timeout means response code is 13
                    else
                    { 
                        // call ProcessValidDataStatus fucntion to process other valid response {send_otp,submit_birthday,send_phonenumber}
                        var processStatusResponse = await ProcessValidDataStatus(otpResponse.data.status, reference, phoneNumber, dateOfBirth);
                        return processStatusResponse;
                    }
                }
                var message = otpResponse.data.message != null ? otpResponse.data.message : "";
                return new TokenizationResponse { Message = otpResponse.message + ", " + message, Status = false };
            }
            var errorMessage = otpResponse.data.message != null ? otpResponse.data.message : "";
            return new TokenizationResponse { Message = otpResponse.message + ", " + errorMessage, Status = false };
        }

        public async Task<TokenizationResponse> ChargeAuthorization(ChargeAuthorization chargeAuthorization)
        {
            try
            {
                // call paystack client
                var httpClient = _httpClientFactory.CreateClient("Paystack");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", $"{Options.SecretKey}");
                HttpContent content = new StringContent(JsonConvert.SerializeObject(chargeAuthorization), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync($"{Options.ChargeAuthorization}", content);

                string apiResponse = await response.Content.ReadAsStringAsync();
                var chargeAuthorizationResponse = JsonConvert.DeserializeObject<ChargeCardResponse>(apiResponse);
                if (response.IsSuccessStatusCode)
                {
                    if (chargeAuthorizationResponse.status == true)
                    {
                        if (chargeAuthorizationResponse.data.status == "success")
                        {
                            return new TokenizationResponse
                            {
                                Type = chargeAuthorizationResponse.data.authorization.card_type,
                                LastDigit = chargeAuthorizationResponse.data.authorization.last4,
                                AuthorizationCode = chargeAuthorizationResponse.data.authorization.authorization_code,
                                Signature = chargeAuthorizationResponse.data.authorization.signature,
                                Message = chargeAuthorizationResponse.data.gateway_response,
                                Status = true,
                                ResponseCode = 0
                            };
                        }
                        // Insufficient Funds
                        else if (chargeAuthorizationResponse.data.status == "failed")
                        {
                            var message1 = chargeAuthorizationResponse.data.gateway_response != null ? chargeAuthorizationResponse.data.gateway_response : "";
                            return new TokenizationResponse { Message = chargeAuthorizationResponse.message + ", " + message1, Status = false, ResponseCode = 10 };
                        }
                    }
                    // Authorization code error
                    var message2 = chargeAuthorizationResponse.data.gateway_response != null ? chargeAuthorizationResponse.data.gateway_response : "";
                    return new TokenizationResponse { Message = chargeAuthorizationResponse.message + ", " + message2, Status = false };
                }
                return new TokenizationResponse { Message = chargeAuthorizationResponse.message, Status = false };
            }
            catch(Exception ex)
            {
                _logger.LogCritical("An error occurred while calling charge transaction", ex);
                return new TokenizationResponse { Message = ex.Message.ToString(), Status = false };
            }            
        }

        private TokenizationResponse ProcessSuccessOrFailedDataStatus(Authorization authorization, string status, string message)
        {
            // check if the status is successfully
            if (status == "success")
            {
                return new TokenizationResponse
                {
                    Type = authorization.card_type,
                    LastDigit = authorization.last4,
                    AuthorizationCode = authorization.authorization_code,
                    Signature = authorization.signature,
                    Message = "Card was tokenize successfully",
                    Status = true,
                    ResponseCode = 0
                };
            }

            // check if the chargeCardResponse.data.status is failed or timeout 
            if (status == "failed" || status == "timeout")
            {
                return new TokenizationResponse
                {
                    Message = message,
                    Status = false,
                    ResponseCode = 0
                };
            }

            // if status is not failed,timeout or successfull return okenization response with response code 13.
            return new TokenizationResponse
            {
                ResponseCode = 13
            };
        }

        private async Task<TokenizationResponse> ProcessValidDataStatus(string status, string reference, string phoneNumber, DateTime birthDate)
        {
            //Check if the data.status was send_otp : Means we need user OTP
            if (status == "send_otp")
            {
                var otpViewModel = new SetOtpViewModel(null, "1234", reference);
                return new TokenizationResponse { Data = otpViewModel, Message = "Please enter your OTP code", Status = true, ResponseCode = 12, };
            }

            //Check if the data.status was send_birthday
            if (status == "send_birthday")
            {
                // if status is Send Birthday, Call SubmitBirthday function.
                var sendBirthday = await SubmitBirthDay(reference, phoneNumber, birthDate);
                return sendBirthday;
            }

            if (status == "send_phone")
            {
                // if status is Submit Phone call submit Phone function
                var submitPhone = await SubmitPhone(reference, phoneNumber, birthDate);
                return submitPhone;
            }
            return new TokenizationResponse { Message = "Please try again later" };
        }

        private async Task<TokenizationResponse> SubmitBirthDay(string reference,string phoneNumber, DateTime birthDate)
        {
            var birthRequest = new SubmitBirthday(birthDate, reference);

            // Call Paystack client
            var httpClient = _httpClientFactory.CreateClient("Paystack");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", $"{Options.SecretKey}");
            HttpContent content = new StringContent(JsonConvert.SerializeObject(birthRequest), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync($"{Options.PayStackSubmitBirthDay}", content);


            string apiResponse = await response.Content.ReadAsStringAsync();
            var birthdayResponse = JsonConvert.DeserializeObject<ChargeCardResponse>(apiResponse);
            if (response.IsSuccessStatusCode)
            {
                if (birthdayResponse.status == true)
                {
                    // call ProcessSuccessOrFailedResult function to process failed,timeout or succees response
                    var processSuccessOrFailedResult = ProcessSuccessOrFailedDataStatus(birthdayResponse.data.authorization, birthdayResponse.data.status,
                        birthdayResponse.message);

                    // check if status is true
                    if (processSuccessOrFailedResult.Status)
                    {
                        return processSuccessOrFailedResult;
                    }
                    // check if status is failed or timeout
                    else if (!processSuccessOrFailedResult.Status && processSuccessOrFailedResult.ResponseCode != 13)
                    {
                        return processSuccessOrFailedResult;
                    }
                    // check if sucess is neither failes, success or timeout means response code is 13
                    else
                    {
                        // call ProcessValidDataStatus fucntion to process other valid response {send_otp,submit_birthday,send_phonenumber}
                        var processStatusResponse = await ProcessValidDataStatus(birthdayResponse.data.status, reference, phoneNumber, birthDate);
                        return processStatusResponse;
                    }                   
                }
                var message = birthdayResponse.data.message != null ? birthdayResponse.data.message : "";
                return new TokenizationResponse { Message = birthdayResponse.message + ", " + message, Status = false };
            }
            var errorMessage = birthdayResponse.data.message != null ? birthdayResponse.data.message : "";
            return new TokenizationResponse { Message = birthdayResponse.message + ", " + errorMessage, Status = false };
        }

        private async Task<TokenizationResponse> SubmitPhone(string reference, string phoneNumber, DateTime birthDate)
        {
            var phoneRequest = new SubmitPhoneNumber(phoneNumber, reference);
            var httpClient = _httpClientFactory.CreateClient("Paystack");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", $"{Options.SecretKey}");
            HttpContent content = new StringContent(JsonConvert.SerializeObject(phoneRequest), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync($"{Options.PayStackSubmitPhone}", content);

            string apiResponse = await response.Content.ReadAsStringAsync();
            var phoneResponse = JsonConvert.DeserializeObject<ChargeCardResponse>(apiResponse);
            if (response.IsSuccessStatusCode)
            {                
                if (phoneResponse.status is true)
                {
                    // call ProcessSuccessOrFailedResult function to process failed,timeout or succees response
                    var processSuccessOrFailedResult = ProcessSuccessOrFailedDataStatus(phoneResponse.data.authorization, phoneResponse.data.status,
                        phoneResponse.message);

                    // check if status is true
                    if (processSuccessOrFailedResult.Status)
                    {
                        return processSuccessOrFailedResult;
                    }
                    // check if status is failed or timeout
                    else if (!processSuccessOrFailedResult.Status && processSuccessOrFailedResult.ResponseCode != 13)
                    {
                        return processSuccessOrFailedResult;
                    }
                    // check if sucess is neither failes, success or timeout means response code is 13
                    else
                    {
                        // call ProcessValidDataStatus fucntion to process other valid response {send_otp,submit_birthday,send_phonenumber}
                        var processStatusResponse = await ProcessValidDataStatus(phoneResponse.data.status, reference, phoneNumber, birthDate);
                        return processStatusResponse;
                    }
                }
                var message = phoneResponse.data.message != null ? phoneResponse.data.message : "";
                return new TokenizationResponse { Message = phoneResponse.message + ", " + message, Status = false };
            }
            var errorMessage = phoneResponse.data.message != null ? phoneResponse.data.message : "";
            return new TokenizationResponse { Message = phoneResponse.message + ", " + errorMessage, Status = false };
        }
    }
}
