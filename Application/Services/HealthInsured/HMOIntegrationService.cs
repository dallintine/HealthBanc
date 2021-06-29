using Application.API_RequestModel.HealthInsured;
using Application.API_ResponseModel.HealthInsured;
using Application.DTO;
using Application.Helpers;
using DataAccess;
using Domain.Models.Axa_Hygeia_Insurance;
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
using System.Threading.Tasks;

namespace Application.Services.HealthInsured
{
    public class HMOIntegrationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<HMOIntegrationService> _logger;
        private readonly IRepositoryWrapper _repoWrapper;

        private AxaMansardConfiguration AxaAccessor { get; }
        private HygeiaConfiguration HygeiaAccessor { get; }

        public HMOIntegrationService(IHttpClientFactory httpClientFactory, IOptions<AxaMansardConfiguration> axaAccessor, IOptions<HygeiaConfiguration> hygeiaAccessor,
            ILogger<HMOIntegrationService> logger,IRepositoryWrapper repoWrapper)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _repoWrapper = repoWrapper;
            HygeiaAccessor = hygeiaAccessor.Value;
            AxaAccessor = axaAccessor.Value;
        }        

        /// <summary>
        /// Get axamansard registration token
        /// </summary>
        /// <returns></returns>
        private async Task<ResponseMessage<AuthenticationResponse>> AxaMansardAuthentication()
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("AxaMansard");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", $"{AxaAccessor.Apikey}");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-api-secret", $"{AxaAccessor.ApiSecret}");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-client-key", $"{AxaAccessor.ClientKey}");

                var response = await httpClient.GetAsync($"{AxaAccessor.AxaMansardToken}");
                if (response.IsSuccessStatusCode)
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    var authResponse = JsonConvert.DeserializeObject<AuthenticationResponse>(apiResponse);
                    if (authResponse.Succeeded)
                    {
                        return new ResponseMessage<AuthenticationResponse> { Data = authResponse, Status = true, Message = "Request was processed successfully" };
                    }
                    return new ResponseMessage<AuthenticationResponse> { Data = authResponse, Status = false, Message = authResponse.Message.ToString() };
                }
                return new ResponseMessage<AuthenticationResponse> { Data = null, Status = false, Message = "Could not make connection" };
            }
            catch (Exception ex)
            {
                _logger.LogCritical("Error occured while trying to get axamansard auth token" + " " + ex.ToString(), ex);
                return new ResponseMessage<AuthenticationResponse> { Data = null, Status = false, Message = "Could not make connection" };
            }

        }

        /// <summary>
        /// Register user to Axamasard HMO
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> AxamansardRegisterUser(EnrollmentModel model)
        {
            try
            {
                model.EntityCode = AxaAccessor.EntityCode;
                var bearerRequest = await AxaMansardAuthentication();
                if (bearerRequest.Status)
                {
                    var httpClient = _httpClientFactory.CreateClient("AxaMansard");
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerRequest.Data.Auth_token);
                    HttpContent content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync($"{AxaAccessor.AxaMansardEnrollement}", content);
                    string apiResponse = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var authResponse = JsonConvert.DeserializeObject<EnrollementResponse>(apiResponse);
                        if (authResponse.success)
                        {
                            return new ResponseMessage { Status = true, Message = authResponse.message };
                        }
                        _logger.LogError("Axamansard Unsuccessfully response : " + apiResponse, authResponse);
                        BackgroundJob.Schedule(() => ResendFailedAxamansardReg(model), DateTime.Now.AddHours(6));
                        return new ResponseMessage { Status = false, Message = authResponse.message };
                    }
                    _logger.LogCritical(" Bad request when trying to register user to axamansard  : " + apiResponse);
                    BackgroundJob.Schedule(() => ResendFailedAxamansardReg(model), DateTime.Now.AddHours(6));
                    return new ResponseMessage { Status = false, Message = "Could not connect to insurance provider. Please try again later" };
                }
                BackgroundJob.Schedule(() => ResendFailedAxamansardReg(model), DateTime.Now.AddHours(6));
                _logger.LogCritical("BadRequest Axamansard :" + bearerRequest.Message);
                return new ResponseMessage { Status = false, Message = bearerRequest.Message };
            }
            catch (Exception ex)
            {
                _logger.LogCritical("An error occurred while enrolling user to axa-mansard" + " " + ex.ToString(), ex);
                BackgroundJob.Schedule(() => ResendFailedAxamansardReg(model), DateTime.Now.AddHours(6));
                return new ResponseMessage { Status = false, Message = "This on us.An error occurred while enrolling user to axa-mansard.Please try again later" };
            }
        }

        /// <summary>
        /// Method to resend failed Axamansard registration
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task ResendFailedAxamansardReg(EnrollmentModel model)
        {
            var response = await AxamansardRegisterUser(model);
            var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByEmail(model.Email);
            var enrollmentModel = await _repoWrapper.EnrollmentOnOnboarding.GetLastEnrollmentByInsuranceProfileId(insuranceProfile.Id);
            if (response.Status)
            {
                enrollmentModel.Status = EnrollmentOnOnboarding_StatusValue.Successful.ToString();
                _repoWrapper.EnrollmentOnOnboarding.Update(enrollmentModel);
                await _repoWrapper.Save();
            }
            else
            {
                enrollmentModel.Message = response.Message;
                _repoWrapper.EnrollmentOnOnboarding.Update(enrollmentModel);
                await _repoWrapper.Save();
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Get hygeia access token
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> HygeiaGetAuthToken()
        {
            try
            {
                var hygeiaAuthModel = new HygeiaAuthModel()
                {
                    username = HygeiaAccessor.Username,
                    password = HygeiaAccessor.Password,
                    grant_type = HygeiaAccessor.GrantType,
                };
                var httpClient = _httpClientFactory.CreateClient("Hygeia");
                var dictionObj = hygeiaAuthModel.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(hygeiaAuthModel).ToString());
                HttpContent content = new FormUrlEncodedContent(dictionObj);
                var response = await httpClient.PostAsync($"{HygeiaAccessor.HygeiaAuth}", content);
                string apiResponse = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var authResponse = JsonConvert.DeserializeObject<HygeiaAuthResponse>(apiResponse);
                    if (authResponse.Access_Token != null)
                    {
                        var encryptedAccesstoken = await _repoWrapper.EncryptedAcessToken.GetEncryptedToken();
                        if (encryptedAccesstoken is null)
                        {
                            _repoWrapper.EncryptedAcessToken.Create(new EncryptedAcessToken { HygeiaAccessToken = authResponse.Access_Token });
                            await _repoWrapper.Save();
                        }
                        else
                        {
                            encryptedAccesstoken.HygeiaAccessToken = authResponse.Access_Token;
                            _repoWrapper.EncryptedAcessToken.Update(encryptedAccesstoken);
                            await _repoWrapper.Save();
                        }
                        RecurringJob.AddOrUpdate(() => HygeiaGetAuthToken(), Cron.HourInterval(12));
                        return new ResponseMessage { Status = true, Message = authResponse.Access_Token };
                    }
                    _logger.LogError("Hygeia Unsuccessfully response : " + apiResponse, authResponse);
                    return new ResponseMessage { Status = false, Message = "" };
                }
                _logger.LogCritical(" Bad request when trying to get hygeia auth token : " + apiResponse);
                return new ResponseMessage { Status = false, Message = "Could not connect to insurance provider. Please try again later" };
            }
            catch (Exception ex)
            {
                _logger.LogCritical("An error occurred while getting hygeia auth token"+ " "+ex.ToString(), ex.ToString());
                return new ResponseMessage { Status = false, Message = "This on us.An error occurred while enrolling user to hygeia.Please try again later" };
            }
        }

        /// <summary>
        /// Register user to Hygeia HMO
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> HygeiaRegisterUser(RegistrationModel model)
        {
            try
            {
                var encrytedAccess = await _repoWrapper.EncryptedAcessToken.GetEncryptedToken();
                string bearerToken;
                if (encrytedAccess == null)
                {
                    var bearerRequest = await HygeiaGetAuthToken();
                    if (!bearerRequest.Status)
                    {
                        BackgroundJob.Schedule(() => ResendFailedHygeiaReg(model), DateTime.Now.AddHours(6));
                        return new ResponseMessage { Status = false, Message = "" };
                    }
                    bearerToken = bearerRequest.Message;
                }
                else
                {
                    bearerToken = encrytedAccess.HygeiaAccessToken;
                }

                model.PlanId = HygeiaAccessor.HygeiaPlanCode;
                model.DataConsent = true;
                var imagePath = model.ImagePath ?? "data:image/png;base64,/9j/4AAQSkZJRgABAQAAAQABAAD/2wCEAAoKCgoKCgsMDAsPEA4QDxYUExMUFiIYGhgaGCIzICUgICUgMy03LCksNy1RQDg4QFFeT0pPXnFlZXGPiI+7u/sBCgoKCgoKCwwMCw8QDhAPFhQTExQWIhgaGBoYIjMgJSAgJSAzLTcsKSw3LVFAODhAUV5PSk9ecWVlcY+Ij7u7+//CABEIBDgB5QMBIgACEQEDEQH/xAAsAAEAAwEBAQAAAAAAAAAAAAAAAgMEAQUGAQEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIQAxAAAALxQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAfQPTHmPTHmPTHmPTHmPTHmPTHmc9QeY9MeY9MeY9Mea9Iea9Iea9Iea9IeY9MeY9MeY9MeZ30h5r0h5r0h5r0h5r0h5r0h5r0h5vPTHmPTHmPTHmPTHmPTHmPTHnPRAEOshdLBeX3efM2sY2MMjYriXPPmbWPQcszQNrHE3OZzSxxNbKNrJE2156jfzmI9FkgbkcZuY+G1kGtjGxgG9iib2CZsY+mtgmbKc8T0AAAAPL9SJRXtiZuah59moUc1CXmeljI6LqTFrvqFWvhibqizHuqMVmnhjj6Mjz5bKzNZqqLMuyJlndEln2RMEtkTNonEztXDP3SM3NfDK1DzrtEjNzWMkdMiwAAAAAAAAAAAAAAAABGQAQmAAAAAEZBGQAAAAIEwAARy6chdXTuKY5dQtYjRbTAsnLKW2xzF+vDsKEB3Tl2FPK6S2yWQ3VdqE48GiiYh3h3Vk1GadMTVT24hVl9MqrrkW5pzJ8h0sr50snRqMU6tR2ebor2+SbL5ZjYADmXWKatYp7aIV3jJdaMl1orheMuoM7QKrQw65dMl1oyW3Cqchk0TFfLRVaGSy8Z69gqo2CmrWKY6BTDSKJWiq0M8NYqnIZLLxj0zAAAympXUaQGKZqUdLlF4ZriaFJpQoNSFBqZ6DezjQrqNKuwMsi/uC80MsjQhwsZomtRaSZbS1mtLGHUWAAAAAAAAhj25SrbRwlqx6zBo4I8ugQ15dJj6sK9OcZPUpgUzugc7PhXZOojKcymU7DmOyw6lIzzhoKIOkE5F1fRlWTO0a6hdm1EwAAAAAAAAAc6AAAAEJgAAAAAAAAAAAAAAAAAAAAAAAhzIbJebM2S87puszZD0J58Z6M82c3RyzJacEy2EKzToxTNfcewycjw0ywj0q8Ok0RydNfMPTZLHw2MfTbHBE9LmLpuywgehzuI2McTbnhw3MkxGA1WZcp6Fnm7C4AAEKtAw26Rho9WoojuGKO8Rx7h59usefPaMdfoDHbfSUaJyMttowy2DJHaMcdwyd1DPDWM0rxi7sGXukY6vRHMmwYp6hm5qHm+lReZYbRHD6Ax6pAAAAjWXHDpw6qsOmM2MWomA5UXKLwcOudABw64OuDrnQA50M951VaDh1VYdZ7zoBw6cOgOdDnQACOHdhO12QJ16KTm3FqNGPViN3m3xJUX0F09FBTrovM0bBRLonzsTnOyK+2iELhVKyorn2ZDtlZX6Xm+iYdFESVU+Dbi2mWXBXzvTs4yJZ5QNCEBdyk5dVpNIAHOgAAAAAAAAAAAAAAAABGQz39DnQAAAAAAAAAAVcLmCRtY7i5XUaWKw0sUTfzHw2M3TT3D021RpNrDYaqaBtYJG0wG9jGxkibWSo9CqjhtYrTRTdmLuYrTYxzNLNSb0ZAAAAAAADy/TxnLr6TFp01Es+yJhnsGGj0Jnce6JTOUzBqlAq7q4V4PUiY2iwxT7eWYd+M5VuGZp4Z4bMwhskYLrZku86Y+a4mZpGWd0iYAAAAAAAAAAAAAAAABEkAjIAAAAAAHDrnQAAAAAAAAAACOa/OLc3C2yWclO7zTT22sRzbiqzmE1zjnL7bPMNde3zi+uVRplGsn3tBZsyajOhoKJ1Vll0Il9SwlWoJdz+gSrhEtrjwsnR0tpSNWbTkJWUUGyuvhbpUGoAAEeTGWy4ZLbhksvEY2DNdMVwvGO28cpvGSWkZJ6BnlcMttoqs6M7QMk9Ar5aK59GSegZ7pCurTwztIojpGay0M2kULxRK0chYAAACFJpZYG1TA0oUmlRWa2DQXoTAAAAAAAAAAAAAAAAAAAAAAAAK/P9LKXY9+cr7ZEp11VlO2jWZbVpn2YNZaAAAAAAAAAAAAAAAAAAAAAAA+MH2b4wfZvjB9m+MH1tHzI+v78ePsqfkx9db8YPs3xg+zfGD7N8YPsHx4+wfHj7GXxg+zfGD7N8YPs3xg+zfGD7N8YPs3xg+zfGD7N8YPs3xg+zfGD7N8YPs3xg+zfGD7N8YPs+fGj7B8ePsHx4AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA//EAAL/2gAMAwEAAgADAAAAIfPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPLDPPPPPPPLDDDDPPPLDDDDDDDPLDDPDPGDJDBCDFNBAMGAGHBCAADCDCBBGFPPPCJOAFDNHAABGMNJPKFKJBAEMKMLFPPPPPPPPDHPPNPPONOOPPCBOMNPPNPMPPPLPJEOMKJACAKGHMMJFCNJFKLFGIBMPPDDHPDKPDLKKDDPDHLLEPLHLPDDPHLPPBHIAHCCMPNCCAMPCHNIADAAPPPPPPPPJCLLNDCNPEPJOPOHEDHCFCNPPPPPPPPDPDDDDAIABDCBDDDPDDDCBDPPMNPPPPCENLCNLAIOCPHGJKMBFLDMBFLAIJPPPAAMEIEMEEIGIIEIEMIIIMAEEAMEAPPPDNNNBHMLENOMNPPOMOJENNPMPNMOOPPINHOGMFIHHNDOBFJPMDJEAJJCBMNOPPHDDDDDDDDDDDDDDDDCEMEDDDDDDDDPPDABBAPMNLBCECADBBGBHPBDHPPPPPPPBBLHOGDJJKNPJIADCCJCBABNPPPPPPPAMMMMMMMMMMIAAEMAMNMMMMMMMMMPPPCPECDDIDHECBCKAJDCPJDDPMCKJHPPPIEEDDPPLHPPLDPLPLDHHPNDHDLHDPPPDDGPLBOPPPPPPPPPPPPPPPPPPPPPPPPICFHKPENPPPPPPPPPPPPPPPPPPPPPPDDDDPPHLDDDPPLDDDDDDDDDDDDDDHPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPP/EAAL/2gAMAwEAAgADAAAAEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIAAAAAEEMMAAAAMMMIAAAAAAEMIMIAAALHOPMOOLMMOCNOrMPPNPNPtMMNOKAAAICIHHMEKFMKHgknIHOCIJLNDGvJKAAAAAAAAMIAACAABABDAANOBAAAAAACAAANnlrvqkiIGELINIOFCOCPCJFHNFANAAAIAEIBEAMFJMAMEIIMBMAEEEIMEAIAANKGECOPDCBMGNCCFKCEOONHAAAAAAAAELLPtFLPApkkpoNGGPLuLljAAAAAAAAMAEMMMPHPOMNOMMMAMMMNOMAADCAAAAIAmrIKtlnrGAAmitNILFCDCMINBOAAAHLPJLLLHDLBDHPHPDHHDLLHCFPDDAAANDBAPLCGKDDBDDDCDCHLDBBCBBDCDAALMtoNJuoAIBtNJNJrlhLHmBtlIDDpAAMIIMIMEMMMEEEMMMMNHHPMIIIAIMMAANNPPNDCMNOOMONMNOJtIIPPKAAAAAAACNOILnhNGFLPmICBvkuFNIDCAAAAAAAPDDDDDDDDCDFPPLDPDCBDDDDDDDDAAABIqKDJNLNDDlEBNNIHALEJLKLPPIAAADHPEEIEMEEIEIMAEEIE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";
                model.ImagePath = imagePath;

                var httpClient = _httpClientFactory.CreateClient("Hygeia");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                //var dictionObj = model.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(model).ToString());
                //dictionObj["ImagePath"] = imagePath;
                //HttpContent content = new FormUrlEncodedContent(dictionObj);
                var jsonString = JsonConvert.SerializeObject(model);
                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync($"{HygeiaAccessor.HygeiaRegistration}", content);
                string apiResponse = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var authResponse = JsonConvert.DeserializeObject<HygeiaRegistrationResponse>(apiResponse);
                    if (authResponse.Success)
                    {
                        return new ResponseMessage { Status = true, Message = authResponse.MemberId };
                    }
                    _logger.LogCritical("Hygeia Unsuccessfully response : " + apiResponse, authResponse);
                    BackgroundJob.Schedule(() => ResendFailedHygeiaReg(model), DateTime.Now.AddHours(6));
                    return new ResponseMessage { Status = false, Message = "" };
                }
                _logger.LogCritical(" Bad request when trying to register user to hygeia  : " + apiResponse);
                BackgroundJob.Schedule(() => ResendFailedHygeiaReg(model), DateTime.Now.AddHours(6));
                return new ResponseMessage { Status = false, Message = "Could not connect to insurance provider. Please try again later" };
            }
            catch (Exception ex)
            {
                _logger.LogCritical("An error occurred while enrolling user to hygeia" + " " + ex.ToString(), ex.ToString());
                BackgroundJob.Schedule(() => ResendFailedHygeiaReg(model), DateTime.Now.AddHours(6));
                return new ResponseMessage { Status = false, Message = "This on us.An error occurred while enrolling user to hygeia.Please try again later" };
            }
        }

        /// <summary>
        /// Deactivate Insurance profile of user using hygeia
        /// </summary>
        /// <param name="enrollNumber"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> HygeiaDeactivateUser(string enrollNumber)
        {
            var encrytedAccess = await _repoWrapper.EncryptedAcessToken.GetEncryptedToken();
            string bearerToken;
            if (encrytedAccess == null)
            {
                var bearerRequest = await HygeiaGetAuthToken();
                if (!bearerRequest.Status)
                {
                    BackgroundJob.Schedule(() => HygeiaDeactivateUser(enrollNumber), DateTime.Now.AddMinutes(60));
                    return new ResponseMessage { Status = false, Message = "" };
                }
                bearerToken = bearerRequest.Message;
            }
            else
            {
                bearerToken = encrytedAccess.HygeiaAccessToken;
            }

            var httpClient = _httpClientFactory.CreateClient("Hygeia");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            var response = await httpClient.PutAsync($"{HygeiaAccessor.HygeiaDeactivate}/{enrollNumber}", null);

            string apiResponse = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var deactivateResponse = JsonConvert.DeserializeObject<HygeiaDeactivateResponse>(apiResponse);
                if (deactivateResponse.Success)
                {
                    return new ResponseMessage { Message = deactivateResponse.Message, Status = true };
                }
                return new ResponseMessage { Message = "User was previously deactivated", Status = false };
            }
            _logger.LogCritical(" Bad request when trying to deactivate user from hygeia  : " + apiResponse);
            BackgroundJob.Schedule(() => HygeiaDeactivateUser(enrollNumber), DateTime.Now.AddMinutes(60));
            return new ResponseMessage { Message = "Could not process Hygeia response", Status = false };
        }
       
        /// <summary>
        /// Method to carry out failed Hygeia registration
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task ResendFailedHygeiaReg(RegistrationModel model)
        {
            var response = await HygeiaRegisterUser(model);
            var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByEmail(model.Email);
            var enrollmentModel = await _repoWrapper.EnrollmentOnOnboarding.GetLastEnrollmentByInsuranceProfileId(insuranceProfile.Id);
            if (response.Status)
            {
                insuranceProfile.TransId = response.Message;
                _repoWrapper.InsuranceProfile.Update(insuranceProfile);

                enrollmentModel.Status = EnrollmentOnOnboarding_StatusValue.Successful.ToString();
                _repoWrapper.EnrollmentOnOnboarding.Update(enrollmentModel);
                await _repoWrapper.Save();
            }
            else
            {
                enrollmentModel.Message = response.Message;
                _repoWrapper.EnrollmentOnOnboarding.Update(enrollmentModel);
                await _repoWrapper.Save();
            }
            await Task.CompletedTask;
        }
    }
}
