using Application.API_RequestModel.HealthInsured;
using Application.API_ResponseModel.HealthInsured;
using Application.DTO;
using Application.Helpers;
using AutoMapper;
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
        private readonly IMapper _mapper;

        private AxaMansardConfiguration AxaAccessor { get; }
        private HygeiaConfiguration HygeiaAccessor { get; }

        public HMOIntegrationService(IHttpClientFactory httpClientFactory, IOptions<AxaMansardConfiguration> axaAccessor, IOptions<HygeiaConfiguration> hygeiaAccessor,
            ILogger<HMOIntegrationService> logger,IRepositoryWrapper repoWrapper, IMapper mapper)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _repoWrapper = repoWrapper;
            _mapper = mapper;
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
                throw new InvalidOperationException("Logfile cannot be read-only");
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
                            return new ResponseMessage { Status = true, Message = authResponse.message , Data= authResponse.code};
                        }
                        _logger.LogError("Axamansard Unsuccessfully response : " + apiResponse, authResponse);
                        BackgroundJob.Schedule(() => ResendFailedAxamansardReg(model), DateTime.Now.AddHours(6));
                        return new ResponseMessage { Status = false, Message = authResponse.message };
                    }
                    _logger.LogCritical(" Bad request when trying to register user to axamansard  : " + apiResponse);
                    BackgroundJob.Schedule(() => ResendFailedAxamansardReg(model), DateTime.Now.AddMilliseconds(6));
                    return new ResponseMessage { Status = false, Message = "Could not connect to insurance provider. Please try again later" };
                }
                BackgroundJob.Schedule(() => ResendFailedAxamansardReg(model), DateTime.Now.AddMilliseconds(6));
                _logger.LogCritical("BadRequest Axamansard :" + bearerRequest.Message);
                return new ResponseMessage { Status = false, Message = bearerRequest.Message };
            }
            catch (Exception ex)
            {
                _logger.LogCritical("An error occurred while enrolling user to axa-mansard" + " " + ex.ToString(), ex);
                BackgroundJob.Schedule(() => ResendFailedAxamansardReg(model), DateTime.Now.AddMilliseconds(6));
                return new ResponseMessage { Status = false, Message = "This on us.An error occurred while enrolling user to axa-mansard.Please try again later" };
            }
        }


        public async Task<ResponseMessage> AxamansardDeactivateUser(string axamansardReference)
        {
            string entityCode = AxaAccessor.EntityCode;
            var axaDeactivation = new AxaDeactivation(axamansardReference, entityCode);
            var bearerRequest = await AxaMansardAuthentication();
            if (bearerRequest.Status)
            {
                var httpClient = _httpClientFactory.CreateClient("AxaMansard");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerRequest.Data.Auth_token);
                HttpContent content = new StringContent(JsonConvert.SerializeObject(axaDeactivation), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync($"{AxaAccessor.AxaMansardDeactivation}", content);
                string apiResponse = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var authResponse = JsonConvert.DeserializeObject<EnrollementResponse>(apiResponse);
                    if (authResponse.success)
                    {
                        return new ResponseMessage { Status = true, Message = authResponse.message };
                    }
                    _logger.LogWarning("Axamansard Unsuccessfully Deactivation response : " + apiResponse, authResponse);
                    BackgroundJob.Schedule(() => AxamansardDeactivateUser(axamansardReference), DateTime.Now.AddHours(6));
                    return new ResponseMessage { Status = false, Message = authResponse.message };
                }
                _logger.LogWarning(" Bad request when trying to deactivate user to axamansard  : " + apiResponse);
                BackgroundJob.Schedule(() => AxamansardDeactivateUser(axamansardReference), DateTime.Now.AddHours(6));
                return new ResponseMessage { Status = false, Message = "Could not connect to insurance provider. Please try again later" };
            }
            BackgroundJob.Schedule(() => AxamansardDeactivateUser(axamansardReference), DateTime.Now.AddHours(6));
            _logger.LogWarning("BadRequest Axamansard Deactivation :" + bearerRequest.Message);
            return new ResponseMessage { Status = false, Message = bearerRequest.Message };
        }

        /// <summary>
        /// Method to resend failed Axamansard registration
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        //[AutomaticRetry(Attempts = 0)]
        public async Task ResendFailedAxamansardReg(EnrollmentModel model)
        {
            model.PlanId ??= "1";
            model.State ??= "Lagos";
            model.Lga ??= "Alimosho";
            model.Hospital ??= "Hamkad";
            var response = await AxamansardRegisterUser(model);
            var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByEmail(model.Email);
            var enrollmentModel = await _repoWrapper.EnrollmentOnOnboarding.GetLastEnrollmentByInsuranceProfileId(insuranceProfile.Id);
            throw new InvalidOperationException("Logfile cannot be read-only");
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
                model.ImagePath ??= "iVBORw0KGgoAAAANSUhEUgAAARMAAAC3CAMAAAAGjUrGAAAAMFBMVEX////f39/d3d38/Pzh4eHs7Ozy8vL4+Pj19fXq6urk5OTv7+/m5ubr6+v29/bn6Of+ydA3AAAEWElEQVR4nO2d2XLDIAxFbcDGW5r//9vGbdM2xU4EKha+6Dx0pm/kjJBYDDSNoiiKoiiKoiiKoiiKoiiKoiiKoiiKohyOtbbv/e2vlW5JGdh5HifTrhjjxrnz0i2Sxi/OfAr54vafc3Nfrxi7tA9CfomZbn1JunkSeLcl5FvMch2kW3g4/WaMPITLMtcVLM+j5Jteup1HspCU3LpQPfl2eNVzfqzM0m09igvZSWuulWQVspFViqtAivfXGCdt6+CTymDoHecuBTxShlghFUiZUpxgS6FX4QegS3Jkev0Bd0jr08JkjRTY3pPupB2l254L4jxnE9RRSlrV+QQ1zXKcOOnG5yGxEn9iMFfeZpYTzHLM6TqonYcTJqgDfJYSTCdenQR0vL7TIhYeVtkBnRxznXTSPyADI0sJpBNL2/yrywkzxaoTdaJOEp0g1mLeFBBzzHbhKYEc2zPHbJBOmPMdSCc9S0nbSrc/C6yBLGIpbphrSqDrsaxJIKgT1l4G6kYgZ9Q2STc+D6zVAgfZd+zE6juQ8x3uGjXiNyjMsb06USc01EkQJoifb3FzLGCYcONkkW5/Dnh76KDfKWmYhLDC5E269XmION8VMCFm2Ia3VnCVbnwmOPNiVCeMagy6GnvDJocJ4lzni6SjbyuwYdKk74/ihkmTuPGFOji5Y8foRIt/bUHsdyj4Z66b2K0v0L2uP8Sto6gTdaJO7kSW4xpSbOQ6ShVlJ7LwgO6d/+XlTWQPcSLd2mOIWqyuIsU2UZ0HdLk+hHhn3Qd1pNgmIlCqCZOIu2Fgl2FDqHEC+W3SDguxGuMuTYeQnWichNQyOmnolwhBnsXYgZpja3LSqZMA8m66dEMPhLwfKN3QAyHPjKUbeiDqJITqpKYcS90LVCcb1LNUQHZS0xyQHCeI39jvQN72qmMXsFkPwVGVrKsFVaTZuE9CjevxrfjYE7XGXaCt2Jm8nPTbihlRrdjZbT/iRbACuZfuB+d4h5qmAertM+vH+LdDwmAx04BiZeiYEfLby4hQnIfkJLJj5exHvvzMvOVx04q7nDfh2vkfssi2lus5rdguQ4z8WFnOt47g+/9LrDtWxnNZseP/JtYdK9N5rLztPDqbxcopSnOfMqVJZ+qKtzLwbghKwExFrybYjnshapoVN5Vamm32UvOEIntQ3uHIa8orzf0sGCNfUsy1pBXtW6eRNvKBKeVmu7T1xEyYpYQXj+U7zV+k063l3hyVAel3oDn3vGREUgr7HrpMCEYK962UbAjuM8ffPXAUcgc6RGY3NKQ6D+Pt3dyYizoJkDpvyruIPC9STjhvNOdGqhqXWolXhKpxsaOTFXUSIuTkTfp3P0PISdRdDEejTkLUSYg62UDms/SyncgMZAtdY7sjMpAteWgvNbiX/tXPUScbSGyrFz20b2XOzZXuRGKAok7O50TkPpmynYjcsZN8MfsxiJzDLXD3/AGJowqlO5EYtHXGlQzjeZp3TSc+LoSs340AAAAASUVORK5CYII=";
                var httpClient = _httpClientFactory.CreateClient("Hygeia");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                // Serialize our concrete class into a JSON String
                var stringPayload = JsonConvert.SerializeObject(model);
                var content = new StringContent(stringPayload, Encoding.UTF8, "application/json");

                //var dictionObj = model.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(model).ToString());
                //HttpContent content = new FormUrlEncodedContent(dictionObj);

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
            BackgroundJob.Schedule(() => HygeiaDeactivateUser(enrollNumber), DateTime.Now.AddHours(6));
            return new ResponseMessage { Message = "Could not process Hygeia response", Status = false };
        }

        /// <summary>
        /// Method to carry out failed Hygeia registration
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [AutomaticRetry(Attempts = 0)]
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

        public async Task<ResponseMessage> EnrollUserToAxamansardOnOnboarding(InsuranceUserProfile insuranceUserProfile)
        {
            // Send user details to axamansard

            var enrollmentModel = _mapper.Map<EnrollmentModel>(insuranceUserProfile);
            if (insuranceUserProfile.InsurancePayeeId != null)
            {
                var payeeInsuranceProfile = await _repoWrapper.InsuranceProfile.GetByIdAsync(insuranceUserProfile.InsurancePayeeId.Value);
                enrollmentModel.Email = payeeInsuranceProfile.Email;
            }
            
            var enrollment = await AxamansardRegisterUser(enrollmentModel);
            if (!enrollment.Status)
            {
                var axaEnrollmentOnOnboarding = new EnrollmentOnOnboarding(insuranceUserProfile.Id, EnrollmentOnOnboarding_StatusValue.Failed.ToString()
                    , enrollment.Message, InsuranceProvider.Axamansard.ToString());
                _repoWrapper.EnrollmentOnOnboarding.Create(axaEnrollmentOnOnboarding);
                await _repoWrapper.Save();
                return new ResponseMessage { Status = false};
            }
            return new ResponseMessage { Status = true,Data=enrollment.Data };
        }

        /// <summary>
        /// Method to register user to hygeia and save all failed enrollments
        /// </summary>
        /// <param name="insuranceUserProfile"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> EnrollUserToHygeiaOnOnboarding(InsuranceUserProfile insuranceUserProfile)
        {
            // Send user details to hygeia
            var registrationModel = _mapper.Map<RegistrationModel>(insuranceUserProfile);
            if (insuranceUserProfile.InsurancePayeeId != null)
            {
                var payeeInsuranceProfile = await _repoWrapper.InsuranceProfile.GetByIdAsync(insuranceUserProfile.InsurancePayeeId.Value);
                registrationModel.Email = payeeInsuranceProfile.Email;
            }
            var registration = await HygeiaRegisterUser(registrationModel);
            if (!registration.Status)
            {
                var enrollmentOnOnboarding = new EnrollmentOnOnboarding(insuranceUserProfile.Id, EnrollmentOnOnboarding_StatusValue.Failed.ToString(), registration.Message,
                    InsuranceProvider.Hygeia.ToString());
                _repoWrapper.EnrollmentOnOnboarding.Create(enrollmentOnOnboarding);
                await _repoWrapper.Save();
                return registration;
            }
            return registration;
        }
    }
}
