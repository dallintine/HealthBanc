using Application.API_RequestModel.HealthInsured;
using Application.API_ResponseModel.HealthInsured;
using Application.DTO;
using Application.Helpers;
using Application.Interfaces;
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
        private readonly IUniqueIdentifier _uniqueIdentifier;

        private AxaMansardConfiguration AxaAccessor { get; }
        private HygeiaConfiguration HygeiaAccessor { get; }
        private SubscriptionDuration SubscriptionAccessor { get; }

        public HMOIntegrationService(IHttpClientFactory httpClientFactory, IOptions<AxaMansardConfiguration> axaAccessor, IOptions<HygeiaConfiguration> hygeiaAccessor,
            ILogger<HMOIntegrationService> logger,IRepositoryWrapper repoWrapper, IMapper mapper,IUniqueIdentifier uniqueIdentifier, IOptions<SubscriptionDuration> subscriptionAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _repoWrapper = repoWrapper;
            _mapper = mapper;
            _uniqueIdentifier = uniqueIdentifier;
            HygeiaAccessor = hygeiaAccessor.Value;
            AxaAccessor = axaAccessor.Value;
            SubscriptionAccessor = subscriptionAccessor.Value;
        }        

        /// <summary>
        /// Get axamansard registration token
        /// </summary>
        /// <returns></returns>
        private async Task<ResponseMessage<AuthenticationResponse>> AxaMansardAuthentication()
        {
            try
            {
                _logger.LogInformation($"Axamansard auth token request\n]");
                var httpClient = _httpClientFactory.CreateClient("AxaMansard");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", $"{AxaAccessor.Apikey}");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-api-secret", $"{AxaAccessor.ApiSecret}");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-client-key", $"{AxaAccessor.ClientKey}");

                var response = await httpClient.GetAsync($"{AxaAccessor.AxaMansardToken}");
                string apiResponse = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Axamansard auth token response [ Response : {apiResponse} \n]");
                if (response.IsSuccessStatusCode)
                {
                    var authResponse = JsonConvert.DeserializeObject<AuthenticationResponse>(apiResponse);
                    if (authResponse.Succeeded)
                    {
                        return new ResponseMessage<AuthenticationResponse> { Data = authResponse, Status = true, Message = "Request was processed successfully" };
                    }
                    return new ResponseMessage<AuthenticationResponse> { Data = authResponse, Status = false, Message = authResponse.Message.ToString() };
                }
                _logger.LogInformation($"Axamansard auth token was not successful [ StatusCode : {response.StatusCode} | Date {DateTime.Now} | Message - Could not make connection]");
                return new ResponseMessage<AuthenticationResponse> { Data = null, Status = false, Message = "Could not make connection" };
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Error occured while trying to get axamansard auth token" + " " + ex.ToString(), ex);
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
                _logger.LogInformation($"Register user to axamansard payload [Payload {JsonConvert.SerializeObject(model)}] /n");
                model.EntityCode = AxaAccessor.EntityCode;
                var bearerRequest = await AxaMansardAuthentication();
                if (bearerRequest.Status)
                {
                    var httpClient = _httpClientFactory.CreateClient("AxaMansard");
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerRequest.Data.Auth_token);
                    HttpContent content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync($"{AxaAccessor.AxaMansardEnrollement}", content);
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Register user on axamasard Response : [Response { apiResponse}  ]");

                    if (response.IsSuccessStatusCode)
                    {
                        var authResponse = JsonConvert.DeserializeObject<EnrollementResponse>(apiResponse);
                        if (authResponse.success)
                        {
                            return new ResponseMessage { Status = true, Message = authResponse.message , Data= authResponse.code};
                        }BackgroundJob.Schedule(() => ResendFailedAxamansardReg(model), DateTime.Now.AddHours(6));
                        return new ResponseMessage { Status = false, Message = authResponse.message };
                    }
                    _logger.LogInformation($"Bad request when trying to register user to axamansard [StatusCode {response.StatusCode} | apiResponse - {apiResponse}");
                    BackgroundJob.Schedule(() => ResendFailedAxamansardReg(model), DateTime.Now.AddHours(6));
                    return new ResponseMessage { Status = false, Message = "Could not connect to insurance provider. Please try again later" };
                }
                BackgroundJob.Schedule(() => ResendFailedAxamansardReg(model), DateTime.Now.AddHours(6));
                _logger.LogInformation($"Get AxamansardAuth Fsailed [Message {bearerRequest.Message}]");
                return new ResponseMessage { Status = false, Message = bearerRequest.Message };
            }
            catch (Exception ex)
            {
                _logger.LogCritical("An error occurred while enrolling user to axa-mansard" + " " + ex.ToString(), ex);
                BackgroundJob.Schedule(() => ResendFailedAxamansardReg(model), DateTime.Now.AddHours(6));
                return new ResponseMessage { Status = false, Message = "This on us.An error occurred while enrolling user to axa-mansard.Please try again later" };
            }
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task<ResponseMessage> AxamansardDeactivateUser(string axamansardReference)
        {
            _logger.LogInformation($"Deactivate user on axamasard : [Reference {axamansardReference}]");
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
                _logger.LogInformation($"Deactivate user on axamasard response : [Response { apiResponse} |Reference {axamansardReference}]");
                if (response.IsSuccessStatusCode)
                {
                    var authResponse = JsonConvert.DeserializeObject<EnrollementResponse>(apiResponse);                   
                    return new ResponseMessage { Status = true, Message = authResponse.message };
                }
                else
                {
                    BackgroundJob.Schedule(() => AxamansardDeactivateUser(axamansardReference), DateTime.Now.AddHours(6));
                    return new ResponseMessage { Status = false, Message = "Could not connect to insurance provider. Please try again later" };
                }               
            }
            BackgroundJob.Schedule(() => AxamansardDeactivateUser(axamansardReference), DateTime.Now.AddHours(6));
            _logger.LogInformation("BadRequest Axamansard Deactivation :" + bearerRequest.Message);
            return new ResponseMessage { Status = false, Message = bearerRequest.Message };
        }

        /// <summary>
        /// Method to resend failed Axamansard registration
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [AutomaticRetry(Attempts = 0)]
        public async Task ResendFailedAxamansardReg(EnrollmentModel model)
        {
            _logger.LogInformation($"Resending Failed Axareg  [Payload {JsonConvert.SerializeObject(model)}]");
            var response = await AxamansardRegisterUser(model);
            if (response.Status)
            {
                var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByEnrolleNumber(model.EnrollmentNo);
                if(insuranceProfile != null)
                {
                    var codeReference = response.Data as string;
                    insuranceProfile.AxamasardReferenceCode = codeReference;
                    _repoWrapper.InsuranceProfile.Update(insuranceProfile);
                    await _repoWrapper.Save();
                }
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
                _logger.LogInformation($"Hygeia Get Token Request]");

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
                _logger.LogInformation($"Hygeia Get Token Response [Response : {apiResponse}] \n");

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
                    return new ResponseMessage { Status = false, Message = "" };
                }
                return new ResponseMessage { Status = false, Message = "Could not connect to insurance provider. Please try again later" };
            }
            catch (Exception ex)
            {
                _logger.LogInformation("An error occurred while getting hygeia auth token"+ " "+ex.ToString(), ex.ToString() + "\n");
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

                _logger.LogInformation($"Register user Hygeia [Payload : {JsonConvert.SerializeObject(model)}] \n");
                model.ImagePath ??= HygeiaAccessor.DefaultImage;
                var httpClient = _httpClientFactory.CreateClient("Hygeia");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                // Serialize our concrete class into a JSON String
                var stringPayload = JsonConvert.SerializeObject(model);
                var content = new StringContent(stringPayload, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync($"{HygeiaAccessor.HygeiaRegistration}", content);
                string apiResponse = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Register user Hygeia  Response [Response : {apiResponse}] \n");
                if (response.IsSuccessStatusCode)
                {
                    var authResponse = JsonConvert.DeserializeObject<HygeiaRegistrationResponse>(apiResponse);
                    if (authResponse.Success)
                    {
                        return new ResponseMessage { Status = true, Message = authResponse.MemberId };
                    }
                    _logger.LogCritical("Hygeia Unsuccessfully response : \n");
                    BackgroundJob.Schedule(() => ResendFailedHygeiaReg(model), DateTime.Now.AddHours(6));
                    return new ResponseMessage { Status = false, Message = "" };
                }
                _logger.LogCritical(" Bad request when trying to register user to hygeia  \n");
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
        [AutomaticRetry(Attempts = 0)]
        public async Task<ResponseMessage> HygeiaDeactivateUser(string enrollNumber)
        {
            _logger.LogInformation($"Deactivate user Hygeia [EnrolleNumber : {enrollNumber}] \n");
            var encrytedAccess = await _repoWrapper.EncryptedAcessToken.GetEncryptedToken();
            string bearerToken;
            if (encrytedAccess == null)
            {
                var bearerRequest = await HygeiaGetAuthToken();
                if (!bearerRequest.Status)
                {
                    BackgroundJob.Schedule(() => HygeiaDeactivateUser(enrollNumber), DateTime.Now.AddHours(60));
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
            _logger.LogInformation($"Deactivate user Hygeia  Response [Response : {apiResponse}] \n");

            if (response.IsSuccessStatusCode)
            {
                var deactivateResponse = JsonConvert.DeserializeObject<HygeiaDeactivateResponse>(apiResponse);
                if (deactivateResponse.Success)
                {
                    _logger.LogInformation($"Deactivate user on axamasard successful : [enrolleNumber {enrollNumber} ]\n");
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
            _logger.LogInformation($"Resend Failed Hygeia Reg [Payload : {JsonConvert.SerializeObject(model)}] \n");
            var response = await HygeiaRegisterUser(model);
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
            if(insuranceUserProfile.FamilyProfileId != null)
            {
                enrollmentModel.Email = insuranceUserProfile.FamilyEmail;
            }
            
            var enrollment = await AxamansardRegisterUser(enrollmentModel);
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
            if (insuranceUserProfile.FamilyProfileId != null)
            {
                registrationModel.Email = insuranceUserProfile.FamilyEmail;
            }
            var registration = await HygeiaRegisterUser(registrationModel);
            return registration;
        }

        public async Task SendDetailsToInsuranceProvider(InsuranceUserProfile insuranceUserProfile)
        {
            _logger.LogInformation($"Sending Details to insurance provider [Payload : {JsonConvert.SerializeObject(insuranceUserProfile)}]\n");
            if (insuranceUserProfile.InsuranceService.ToLower() == InsuranceProvider.Hygeia.ToString().ToLower())
            {
                var response = await EnrollUserToHygeiaOnOnboarding(insuranceUserProfile);
                if (response.Status)
                {
                    insuranceUserProfile.TransId = response.Message;
                }
                else
                {
                    if (insuranceUserProfile.TransId is null)
                    {
                        insuranceUserProfile.TransId = "Pending";
                    }
                }
            }
            else
            {
                var axaRegResponse = await EnrollUserToAxamansardOnOnboarding(insuranceUserProfile);
                var codeReference = axaRegResponse.Data as string;
                insuranceUserProfile.AxamasardReferenceCode = codeReference;
            }
            _repoWrapper.InsuranceProfile.Update(insuranceUserProfile);
            await _repoWrapper.Save();
        }

        public async Task OnboardCompanyUsersToHMO(int companyUserId, string status, DateTime endActiveStatusDate, string insuranceProvider)
        {
            _logger.LogInformation($"Onboarding Company Users To HMO [Provider :{insuranceProvider}] | tatus : {status} | CompanyUserId : {companyUserId} \n");
            var insuranceProfiles = await _repoWrapper.InsuranceProfile.QueryableInsuranceProfilesUnderCompany(companyUserId);
            var insuranceUserProfiles = new List<InsuranceUserProfile>();
            // Onboard users with an active company subscription status
            if (status is null)
            {
                insuranceUserProfiles = insuranceProfiles.Where(x => x.CompanySubscribedStatus == InsuranceProfile_CompanySubStatusValue.Pending.ToString() ||
                x.CompanySubscribedStatus == InsuranceProfile_CompanySubStatusValue.Active.ToString()).ToList();
            }
            // Onboard users with a pending company subscription status
            else
            {
                insuranceUserProfiles = insuranceProfiles.Where(x => x.CompanySubscribedStatus == status).ToList();
            }

            foreach (var item in insuranceUserProfiles)
            {
                if (insuranceProvider.ToLower() == InsuranceProvider.Hygeia.ToString().ToLower())
                {
                    var result = await EnrollUserToHygeiaOnOnboarding(item);
                    if (result.Status)
                    {
                        item.TransId = result.Message;
                    }
                }
                else
                {
                    item.TransId = _uniqueIdentifier.GetUniqueCode(10);
                    await EnrollUserToAxamansardOnOnboarding(item);
                }
                item.ActiveStatus = true;
                item.SubscriptionStatus = true;
                item.StartActiveStatusDate = endActiveStatusDate.AddDays(-SubscriptionAccessor.FreeTrialDayDuration);
                item.EndActiveStatusDate = endActiveStatusDate;
                item.CompanySubscribedStatus = InsuranceProfile_CompanySubStatusValue.Active.ToString();
                _repoWrapper.InsuranceProfile.Update(item);
            }
            await _repoWrapper.Save();
            await Task.CompletedTask;
        }
    }
}
