using Application.API_RequestModel.Paystack;
using Application.API_ResponseModel.Paystack;
using Application.Helpers;
using Application.Helpers.ThirdPartyAPI;
using Application.ViewModels.Paystack;
using DataAccess;
using DataAccess.HealthInsured.Interfaces;
using Domain.Models;
using Domain.Models.Axa.Hygeia_Insurance;
using Domain.Models.Axa_Hygeia_Insurance;
using Domain.Models.ReportAndLogs;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services.Paystack
{
    public class PaystackService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PaystackService> _logger;
        private readonly IRepositoryWrapper _repoWrapper;

        private Helpers.Paystack Options { get; }

        public PaystackService(IHttpClientFactory httpClientFactory,IOptions<Helpers.Paystack> paystackAccessor,
            ILogger<PaystackService> logger,IRepositoryWrapper repoWrapper)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _repoWrapper = repoWrapper;
            Options = paystackAccessor.Value;
        }
        
        public async Task<TokenizationResponse> ChargeCard(ChargeCard chargeCard, int id,string phoneNumber,DateTime dateofBirth)
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
                        chargeCardResponse.message, chargeCardResponse.data.reference);

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
                        // call ProcessValidDataStatus fucntion to process other valid response {send_otp,submit_birthday,send_phonenumber}
                        var processStatusResponse = await ProcessValidDataStatus(chargeCardResponse.data.status, chargeCard.reference, phoneNumber
                            , dateofBirth,chargeCard.pin);
                        //Check if reposne is open_url
                        if(processStatusResponse.ResponseCode == 20 && processStatusResponse.Status)
                        {
                            processStatusResponse.RedirectUrl = chargeCardResponse.data.url;
                            return processStatusResponse;
                        }
                        return processStatusResponse;
                    }  
                }
                var message = chargeCardResponse.data.message ?? "";
                var gatewayResponse = chargeCardResponse.data.gateway_response ?? "";
                return new TokenizationResponse { Message = chargeCardResponse.message + ", " + message+","+ gatewayResponse, Status = false };
            }
            string errorMessage;
            try
            {
                errorMessage = chargeCardResponse.data.message.ToString();
            }
            catch (Exception)
            {
                errorMessage = "";
            }
            return new TokenizationResponse { Message = chargeCardResponse.message + ", " + errorMessage, Status = false };
        }
        public async Task<TokenizationResponse> SendOtp(string otp, string reference,string phoneNumber, DateTime dateOfBirth,string pin)
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
                        otpResponse.message, otpResponse.data.reference);

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
                        var processStatusResponse = await ProcessValidDataStatus(otpResponse.data.status, reference, phoneNumber, dateOfBirth,pin);
                        //Check if reposne is open_url
                        if (processStatusResponse.ResponseCode == 20 && processStatusResponse.Status)
                        {
                            processStatusResponse.RedirectUrl = otpResponse.data.url;
                            return processStatusResponse;
                        }
                        return processStatusResponse;
                    }
                }
                var message = otpResponse.data.message ?? "";
                var gatewayResponse = otpResponse.data.gateway_response ?? "";
                return new TokenizationResponse { Message = otpResponse.message + ", " + message+","+gatewayResponse, Status = false };
            }
            var errorMessage = otpResponse.data.message ?? "";
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
                                Reference = chargeAuthorizationResponse.data.reference,
                                ResponseCode = 0
                            };
                        }
                        // Insufficient Funds
                        else if (chargeAuthorizationResponse.data.status == "failed")
                        {
                            var message1 = chargeAuthorizationResponse.data.gateway_response ?? "";
                            return new TokenizationResponse { Message = chargeAuthorizationResponse.message + ", " + message1, Status = false, ResponseCode = 10 };
                        }
                    }
                    var message2 = chargeAuthorizationResponse.data.gateway_response ?? "";
                    return new TokenizationResponse { Message = chargeAuthorizationResponse.message + ", " + message2, Status = false };
                }
                // Authorization code error
                return new TokenizationResponse { Message = chargeAuthorizationResponse.message, Status = false };
            }
            catch(Exception ex)
            {
                _logger.LogCritical("An error occurred while calling charge transaction", ex);
                return new TokenizationResponse { Message = ex.Message.ToString(), Status = false };
            }            
        }
        private TokenizationResponse ProcessSuccessOrFailedDataStatus(Authorization authorization, string status, string message,string reference)
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
                    Message = "Card was tokenized successfully",
                    Status = true,
                    Reference = reference,
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
        private async Task<TokenizationResponse> ProcessValidDataStatus(string status, string reference, string phoneNumber, DateTime birthDate,string pin)
        {
            //Check if the data.status was send_otp : Means we need user OTP
            if (status == "send_otp")
            {
                var otpViewModel = new SetOtpViewModel(null, pin, reference);
                return new TokenizationResponse { Data = otpViewModel, Message = "Please enter your OTP code", Status = true, ResponseCode = 12, };
            }

            //Check if the data.status was send_birthday
            if (status == "send_birthday")
            {
                // if status is Send Birthday, Call SubmitBirthday function.
                var sendBirthday = await SubmitBirthDay(reference, phoneNumber, birthDate,pin);
                return sendBirthday;
            }
            if (status == "open_url")
            {
                // if status is open url
                return new TokenizationResponse
                {
                    Message = "Redirect to Redirect Url",
                    Status = true,
                    Reference = reference,
                    ResponseCode = 20,
                };
            }
            if(status == "pending")
            {
                Thread.Sleep(11000);
                var pending = await CheckPendingCharge(reference, phoneNumber, birthDate, pin);
                return pending;
            }
            if (status == "send_phone")
            {
                // if status is Submit Phone call submit Phone function
                var submitPhone = await SubmitPhone(reference, phoneNumber, birthDate,pin);
                return submitPhone;
            }
            return new TokenizationResponse { Message = "Please try again later" };
        }
        private async Task<TokenizationResponse> SubmitBirthDay(string reference,string phoneNumber, DateTime birthDate,string pin)
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
                        birthdayResponse.message, birthdayResponse.data.reference);

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
                        var processStatusResponse = await ProcessValidDataStatus(birthdayResponse.data.status, reference, phoneNumber, birthDate,pin);
                        //Check if reposne is open_url
                        if (processStatusResponse.ResponseCode == 20 && processStatusResponse.Status)
                        {
                            processStatusResponse.RedirectUrl = birthdayResponse.data.url;
                            return processStatusResponse;
                        }
                        return processStatusResponse;
                    }                   
                }
                var message = birthdayResponse.data.message ?? "";
                return new TokenizationResponse { Message = birthdayResponse.message + ", " + message, Status = false };
            }
            var errorMessage = birthdayResponse.data.message ?? "";
            return new TokenizationResponse { Message = birthdayResponse.message + ", " + errorMessage, Status = false };
        }
        private async Task<TokenizationResponse> SubmitPhone(string reference, string phoneNumber, DateTime birthDate,string pin)
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
                        phoneResponse.message, phoneResponse.data.reference);

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
                        var processStatusResponse = await ProcessValidDataStatus(phoneResponse.data.status, reference, phoneNumber, birthDate,pin);
                        //Check if reposne is open_url
                        if (processStatusResponse.ResponseCode == 20 && processStatusResponse.Status)
                        {
                            processStatusResponse.RedirectUrl = phoneResponse.data.url;
                            return processStatusResponse;
                        }
                        return processStatusResponse;
                    }
                }
                _logger.LogWarning(apiResponse, phoneResponse.ToString());
                return new TokenizationResponse { Message = phoneResponse.message, Status = false };
            }
            //var errorMessage = phoneResponse.data.message ?? "";
            return new TokenizationResponse { Message = phoneResponse.message, Status = false };
        }
        public async Task<TokenizationResponse> VerifyTransaction(string reference)
        {
            var httpClient = _httpClientFactory.CreateClient("Paystack");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", $"{Options.SecretKey}");
            var response = await httpClient.GetAsync($"{Options.VerifyTransaction}/{reference}");
            if (response.IsSuccessStatusCode)
            {
                string apiResponse = await response.Content.ReadAsStringAsync();
                var verifyResponse = JsonConvert.DeserializeObject<ChargeCardResponse>(apiResponse);
                if (verifyResponse.status == true)
                {
                    if(verifyResponse.data.status == "success")
                    {
                        return new TokenizationResponse
                        {
                            Type = verifyResponse.data.authorization.card_type,
                            LastDigit = verifyResponse.data.authorization.last4,
                            AuthorizationCode = verifyResponse.data.authorization.authorization_code,
                            Signature = verifyResponse.data.authorization.signature,
                            Message = "Card was tokenized successfully",
                            Status = true,
                            Reference = reference,
                            ResponseCode = 0
                        };
                    }
                    else
                    {
                        return new TokenizationResponse
                        {
                            Type = verifyResponse.data.authorization.card_type,
                            LastDigit = verifyResponse.data.authorization.last4,
                            AuthorizationCode = verifyResponse.data.authorization.authorization_code,
                            Signature = verifyResponse.data.authorization.signature,
                            Message = verifyResponse.data.gateway_response,
                            Status = false,
                            Reference = reference,
                            ResponseCode = 10
                        };
                    }
                }
                return new TokenizationResponse
                {
                    Type = verifyResponse.data.authorization.card_type,
                    LastDigit = verifyResponse.data.authorization.last4,
                    AuthorizationCode = verifyResponse.data.authorization.authorization_code,
                    Signature = verifyResponse.data.authorization.signature,
                    Message = verifyResponse.data.gateway_response,
                    Status = false,
                    Reference = reference,
                    ResponseCode = 5
                };
            }
            return new TokenizationResponse
            {
                Message = "Could not connect to paystack service, please try again later",
                Status = false,
                Reference = reference,
                ResponseCode = 5
            };
        }
        private async Task<TokenizationResponse> CheckPendingCharge(string reference,string phoneNumber, DateTime birthDate,string pin)
        {
            var httpClient = _httpClientFactory.CreateClient("Paystack");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", $"{Options.SecretKey}");
            var response = await httpClient.GetAsync($"{Options.PayStackChargeCard}/{reference}");

            string apiResponse = await response.Content.ReadAsStringAsync();
            var verifyResponse = JsonConvert.DeserializeObject<ChargeCardResponse>(apiResponse);

            if (response.IsSuccessStatusCode)
            {               
                if (verifyResponse.status == true)
                {
                    // call ProcessSuccessOrFailedResult function to process failed,timeout or succees response
                    var processSuccessOrFailedResult = ProcessSuccessOrFailedDataStatus(verifyResponse.data.authorization, verifyResponse.data.status,
                        verifyResponse.message, verifyResponse.data.reference);

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
                        // call ProcessValidDataStatus fucntion to process other valid response {send_otp,submit_birthday,send_phonenumber,pending}
                        var processStatusResponse = await ProcessValidDataStatus(verifyResponse.data.status, reference, phoneNumber, birthDate, pin);
                        //Check if reposne is open_url
                        if (processStatusResponse.ResponseCode == 20 && processStatusResponse.Status)
                        {
                            processStatusResponse.RedirectUrl = verifyResponse.data.url;
                            return processStatusResponse;
                        }
                        return processStatusResponse;
                    }
                }
                var message = verifyResponse.data.message ?? "";
                var gatewayResponse = verifyResponse.data.gateway_response ?? "";
                return new TokenizationResponse { Message = verifyResponse.message + ", " + message+","+gatewayResponse, Status = false };
            }
            var errorMessage = verifyResponse.data.message ?? "";
            return new TokenizationResponse { Message = verifyResponse.message + ", " + errorMessage, Status = false };
        }        

        /// <summary>
        /// Method to process refund of paystack made via paystack
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public async Task RefundTestCardFunds(string reference, string amount)
        {
            var refund = new Refund(reference, amount);
            var httpClient = _httpClientFactory.CreateClient("Paystack");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", $"{Options.SecretKey}");
            HttpContent content = new StringContent(JsonConvert.SerializeObject(refund), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync($"{Options.Refund}", content);

            string apiResponse = await response.Content.ReadAsStringAsync();
            var refundResponse = JsonConvert.DeserializeObject<ChargeCardResponse>(apiResponse);
            if (response.IsSuccessStatusCode)
            {
                if (refundResponse.status is true)
                {
                    await Task.CompletedTask;
                }
            }
            await Task.CompletedTask;
        }       
    }
}
