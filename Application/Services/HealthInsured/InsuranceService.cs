using Application.API_RequestModel.HealthInsured;
using Application.API_ResponseModel.HealthInsured;
using Application.AuditAndReport.AuditLog;
using Application.DTO;
using Application.DTO.HealthInsured_AxaMansard;
using Application.Helpers;
using Application.Interfaces;
using Application.Services.Identity;
using Application.ViewModels;
using Application.ViewModels.HealthInsured;
using AutoMapper;
using DataAccess;
using Domain.Models;
using Domain.Models.Axa.Hygeia_Insurance;
using Domain.Models.Axa_Hygeia_Insurance;
using Domain.Models.ReportAndLogs;
using Hangfire;
using HealthBanc.DTO.HealthInsured_AxaMansard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.HealthInsured
{
    public class InsuranceService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMapper _mapper;
        private readonly AuditLogService _auditLogServices;
        private readonly ILogger<InsuranceService> _logger;
        private readonly IUniqueIdentifier _uniqueIdentifier;
        private readonly IEmailSender _emailSender;
        private readonly IFileProcessor _fileProcessor;
        private readonly IdentityService _identityService;
        private readonly IRepositoryWrapper _repoWrapper;

        private AxaMansardConfiguration Options { get; }
        private HygeiaConfiguration _hygeiaAccessor { get; }
        private SubscriptionDuration _subscriptionAccessor { get; }


        public InsuranceService(IHttpClientFactory httpClientFactory, IOptions<AxaMansardConfiguration> axaAccessor, IMapper mapper, AuditLogService auditLogServices,
            ILogger<InsuranceService> logger, IUniqueIdentifier uniqueIdentifier, IEmailSender emailSender, IdentityService identityService,
             IOptions<HygeiaConfiguration> hygeiaAccessor, IFileProcessor fileProcessor, IRepositoryWrapper repoWrapper,
             IOptions<SubscriptionDuration> subscriptionAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _mapper = mapper;
            _auditLogServices = auditLogServices;
            _logger = logger;
            _uniqueIdentifier = uniqueIdentifier;
            _emailSender = emailSender;
            _fileProcessor = fileProcessor;
            _identityService = identityService;
            Options = axaAccessor.Value;
            _hygeiaAccessor = hygeiaAccessor.Value;
            _repoWrapper = repoWrapper;
            _subscriptionAccessor = subscriptionAccessor.Value ;
        }

        public async Task<ResponseMessage> UserOnboarding(UserProfileviewModel userProfile, int userId,string ipAddress,string device)
        {
            var user = await _repoWrapper.ApplicationUser.FindByIdAsync(userId);

            var checkIfUserHasBeenProfiled = await _repoWrapper.InsuranceProfile.GetByUserIdAsync(userId);
            if (checkIfUserHasBeenProfiled != null) return new ResponseMessage { Message = "User has a profile already" };
            
            var creatResponse = await CreateUserProfile(userProfile, user);
            if (creatResponse.Status)
            {
                var auditViewModel = new AuditLogViewModel(userId, null, null, "Created HealthInsured profile", "Created HealthInsured profile");
                BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel, ipAddress, device));

                return new ResponseMessage
                {
                    Data = new AxaResponse() { IsSuccessful = "True", Message = "Profile was created successfully" },
                    Message = "Profile was created successfully",
                    Status = true
                };
            }
            return new ResponseMessage { Status = false, Message = "Could not create profile,please try again later" };
        }

        public async Task<ResponseMessage> CreateUserProfile(UserProfileviewModel userProfile,ApplicationUser user)
        {
            var checkIfUserIsRegisteredAsCorporateUser = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(user.Id);
            if (!(checkIfUserIsRegisteredAsCorporateUser is null)) return new ResponseMessage { Status = false, Message = "Üser is registered as corporate user" };

            userProfile.InsuranceService ??= InsuranceProvider.Axamansard.ToString();

            var profile = _mapper.Map<InsuranceUserProfile>(userProfile);
            profile.InsuranceService = userProfile.InsuranceService == "axamansard" ? InsuranceProvider.Axamansard.ToString() : InsuranceProvider.Hygeia.ToString();
            profile.UserId = user.Id;

            profile.CareProviderName = userProfile.CareProviderName.Split(":")[0]; profile.CPAddress = userProfile.CareProviderName.Split(":")[1];

            profile.TransId = (userProfile.InsuranceService == null | userProfile.InsuranceService == InsuranceProvider.Axamansard.ToString()) ? _uniqueIdentifier.GetUniqueCode(12) : "";
            profile.CPCity = userProfile.CareProviderName.Split(":").Length == 3 ? userProfile.CareProviderName.Split(":")[2] : "";
            profile.InsuranceService = userProfile.InsuranceService;

            var updatedProfile = _mapper.Map(user, profile);

            _repoWrapper.InsuranceProfile.Create(updatedProfile);

            var completionProfile = new InsuranceCompletionProfile(user.Id, true, false,userProfile.InsuranceService);
            _repoWrapper.InsuranceCompletionProfile.Create(completionProfile);
            await _repoWrapper.Save();

            var newServiceString = user.ServiceUsed + "HealthInsured,";
            user.ServiceUsed = newServiceString;
            _repoWrapper.ApplicationUser.Update(user);
            await _repoWrapper.Save();
            return new ResponseMessage { Status = true};
        }

        public async Task<ResponseMessage> UpdateProfileAsync(UpdateProfileViewModel updateProfileViewModel,int userId)
        {
            var checkIfUserHasBeenProfiled = await _repoWrapper.InsuranceProfile.GetByUserIdAsync(userId);
            if (checkIfUserHasBeenProfiled == null) return new ResponseMessage { Message = "User does not have a profile", Status=false };

            var updatedProfile = _mapper.Map(updateProfileViewModel, checkIfUserHasBeenProfiled);
            try
            {
                updatedProfile.CareProviderName = updateProfileViewModel.CareProviderName.Split(':')[0];
                updatedProfile.CPAddress = updateProfileViewModel.CareProviderName.Split(':')[1];
                updatedProfile.CPCity = updateProfileViewModel.CareProviderName.Split(":").Length == 3 ? updateProfileViewModel.CareProviderName.Split(":")[2] : "";
            }
            catch (Exception ex)
            {
                updatedProfile.CareProviderName = updateProfileViewModel.CareProviderName;
            }
            _repoWrapper.InsuranceProfile.Update(updatedProfile);

            var activityLog = new ActivityLog(checkIfUserHasBeenProfiled.Id, null, "Updated HealthInsured Profile", ServiceNames.HealthInsured.ToString());
            _repoWrapper.ActivityLog.Create(activityLog);
            await _repoWrapper.Save();
            return new ResponseMessage { Status = true, Message = "Profile was updated successfully" };
        }

        public async Task<ResponseMessage> GetPaginatedInsuranceProfiles(PaginationQuery paginationQuery)
        {
            var insuranceProfiles =await _repoWrapper.InsuranceProfile.GetPaginatedInsuranceUserProfiles(paginationQuery);

            var insuranceProfilesDTO = _mapper.Map<IEnumerable<InsuranceUserProfile>,IEnumerable<IndividualProfileDTO>>(insuranceProfiles.Data);

            var paginatedResponse = new PagedResponse<IndividualProfileDTO>
            {
                Data = insuranceProfilesDTO,
                PageNumber = paginationQuery.PageNumber >= 1 ? paginationQuery.PageNumber : (int?)null,
                PageSize = paginationQuery.PageSize >= 1 ? paginationQuery.PageSize : (int?)null,
                RecordCount = insuranceProfiles.RecordCount,
                PageCount = insuranceProfiles.PageCount
            };
            return new ResponseMessage { Data = paginatedResponse, Status = true };
        }

        public async Task<ResponseMessage> GetPaginatedTransactionLogs(PaginationQuery paginationQuery,string email)
        {
            var transactionLogs = await _repoWrapper.PaymentReference.GetPaginatedPaymentReference(paginationQuery,email);

            var transactionLogDTO = _mapper.Map<IEnumerable<PaymentReference>, IEnumerable<TransactionLogDTO>>(transactionLogs.Data);

            var paginatedResponse = new PagedResponse<TransactionLogDTO>
            {
                Data = transactionLogDTO,
                PageNumber = paginationQuery.PageNumber >= 1 ? paginationQuery.PageNumber : (int?)null,
                PageSize = paginationQuery.PageSize >= 1 ? paginationQuery.PageSize : (int?)null,
                RecordCount = transactionLogs.RecordCount,
                PageCount = transactionLogs.PageCount
            };
            return new ResponseMessage { Data = paginatedResponse, Status = true };
        }

        public async Task<ResponseMessage> GetExtendedInsuranceProfileDetailByEmail(string email)
        {
            var insuranceProfile = await _repoWrapper.InsuranceProfile.GetExtendedProfileDetailsByEmail(email);
            if (insuranceProfile is null) return new ResponseMessage { Status = false, Message = "Profile with the email was not found" };

            var insuranceProfileDTO = _mapper.Map<IndividualProfileDTO>(insuranceProfile);
            return new ResponseMessage { Data = insuranceProfileDTO, Status = true };
        }

        public async Task EnrollUserToAxamansardOnOnboarding(InsuranceUserProfile insuranceUserProfile)
        {
            // Send user details to axamansard
            var enrollmentModel = _mapper.Map<EnrollmentModel>(insuranceUserProfile);
            var enrollment = await AxamansardRegisterUser(enrollmentModel);
            if (!enrollment.Status)
            {
                var axaEnrollmentOnOnboarding = new EnrollmentOnOnboarding(insuranceUserProfile.UserId, insuranceUserProfile.Id, EnrollmentOnOnboarding_StatusValue.Failed.ToString()
                    , enrollment.Message, "axamansard");
                _repoWrapper.EnrollmentOnOnboarding.Create(axaEnrollmentOnOnboarding);
                await _repoWrapper.Save();
            }
            await Task.CompletedTask;
        }

        public async Task<ResponseMessage> AxamansardRegisterUser(EnrollmentModel model)
        {
            try
            {
                model.EntityCode = Options.EntityCode;
                var bearerRequest = await AxaMansardAuthentication();
                if (bearerRequest.Status)
                {
                    // var httpClient = _httpClientFactory.CreateClient("AxaMansard");
                    //httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerRequest.Data.Auth_token);
                    //HttpContent content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");
                    //var response = await httpClient.PostAsync($"{Options.AxaMansardEnrollement}", content);

                    //if (response.IsSuccessStatusCode)
                    //{
                    //string apiResponse = await response.Content.ReadAsStringAsync();
                    //var authResponse = JsonConvert.DeserializeObject<EnrollementResponse>(apiResponse);
                    //if (authResponse.success)
                    //{
                    //    return new ResponseMessage { Status = true, Message = authResponse.message };
                    //}
                    //return new ResponseMessage { Status = true, Message = authResponse.message };
                    //}
                    //return new ResponseMessage { Status = false, Message = "Could not connect to insurance provider. Please try again later" };
                    return new ResponseMessage { Status = true, Message = "Successful" };
                }
                return new ResponseMessage { Status = false, Message = bearerRequest.Message };
            }
            catch (Exception ex)
            {
                _logger.LogCritical("An error occurred while enrolling user to axa-mansard", ex);
                return new ResponseMessage { Status = false, Message = "This on us.An error occurred while enrolling user to axa-mansard.Please try again later" };
            }
        }

        private async Task<ResponseMessage<AuthenticationResponse>> AxaMansardAuthentication()
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("AxaMansard");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", $"{Options.Apikey}");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-api-secret", $"{Options.ApiSecret}");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-client-key", $"{Options.ClientKey}");

                var response = await httpClient.GetAsync($"{Options.AxaMansardToken}");
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
                _logger.LogCritical("Error occured while trying to get axamansard auth token", ex);
                return new ResponseMessage<AuthenticationResponse> { Data = null, Status = false, Message = "Could not make connection" };
            }

        }

        public async Task<ResponseMessage> EnrollUserToHygeiaOnOnboarding(InsuranceUserProfile insuranceUserProfile)
        {
            // Send user details to hygeia
            var registrationModel = _mapper.Map<RegistrationModel>(insuranceUserProfile);
            var registration = await HygeiaRegisterUser(registrationModel);
            if (!registration.Status)
            {
                var enrollmentOnOnboarding = new EnrollmentOnOnboarding(insuranceUserProfile.UserId, insuranceUserProfile.Id,EnrollmentOnOnboarding_StatusValue.Failed.ToString(), registration.Message,
                    "hygeia");
                _repoWrapper.EnrollmentOnOnboarding.Create(enrollmentOnOnboarding);
                await _repoWrapper.Save();
                return registration;
            }
            return registration;
        }

        public async Task<ResponseMessage> HygeiaRegisterUser(RegistrationModel model)
        {
            try
            {
                model.PlanId = _hygeiaAccessor.HygeiaPlanCode;
                var httpClient = _httpClientFactory.CreateClient("Hygeia");
                var dictionObj = model.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(model).ToString());
                HttpContent content = new FormUrlEncodedContent(dictionObj);
                var response = await httpClient.PostAsync($"{_hygeiaAccessor.HygeiaRegistration}", content);
                if (response.IsSuccessStatusCode)
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    var authResponse = JsonConvert.DeserializeObject<HygeiaRegistrationResponse>(apiResponse);
                    if (authResponse.Success)
                    {
                        return new ResponseMessage { Status = true, Message = authResponse.MemberId };
                    }
                    return new ResponseMessage { Status = false, Message = "" };
                }
                return new ResponseMessage { Status = false, Message = "Could not connect to insurance provider. Please try again later" };
            }
            catch (Exception ex)
            {
                //_logger.LogCritical("An error occurred while enrolling user to axa-mansard", ex);
                return new ResponseMessage { Status = false, Message = "This on us.An error occurred while enrolling user to hygeia.Please try again later" };
            }
        }

        public async Task<ResponseMessage> HygeiaDeactivateUser(string enrollNumber)
        {
            var httpClient = _httpClientFactory.CreateClient("Hygeia");
            var response = await httpClient.PutAsync($"{_hygeiaAccessor.HygeiaDeactivate}/{enrollNumber}", null);

            string apiResponse = await response.Content.ReadAsStringAsync();
            var deactivateResponse = JsonConvert.DeserializeObject<HygeiaDeactivateResponse>(apiResponse);
            if (response.IsSuccessStatusCode)
            {
                if (deactivateResponse.Success)
                {
                    return new ResponseMessage { Message = deactivateResponse.Message, Status = true };
                }
                return new ResponseMessage { Message = "User was previously deactivated", Status = false };
            }
            return new ResponseMessage { Message = "Could not process Hygeia response", Status = false };
        }

        public async Task<ResponseMessage<HealthInsuredProfileStateDTO>> GetProfileCompletion(int userId)
        {
            var profile = await _repoWrapper.InsuranceCompletionProfile.GetCompletionStateByUserId(userId);
            if (profile == null)
            {
                var corporateUser = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(userId);
                if (corporateUser != null)
                {
                    var corporateProfileState = new HealthInsuredProfileStateDTO(true, corporateUser.EmailConfirmed, corporateUser.ProfileCompleted
                        , corporateUser.TokenizationCompleted, null);
                    return new ResponseMessage<HealthInsuredProfileStateDTO>
                    {
                        Data = corporateProfileState,
                        Status = true,
                        Message = "Profile completion state was fetched successfully"
                    };
                }
                var notFoundProfileState = new HealthInsuredProfileStateDTO(null, null, false, false, null);
                return new ResponseMessage<HealthInsuredProfileStateDTO>
                {
                    Data = notFoundProfileState,
                    Status = true,
                    Message = "Profile completion state was fetched successfully"
                };
            };
            var individualProfileState = new HealthInsuredProfileStateDTO(false, null, profile.ProfileCompleted, profile.TokenizationCompleted, profile.ServiceUsed);
            return new ResponseMessage<HealthInsuredProfileStateDTO>
            {
                Data = individualProfileState,
                Status = true,
                Message = "Profile completion state was fetched successfully"
            };
        }

        public ResponseMessage GetTowns(string state, string insuranceProvider)
        {
            var townListDTO = new CityListDTO();
            if (insuranceProvider.ToLowerInvariant() == "axamansard")
            {
                var townList = _repoWrapper.AxaMansardHospitalList.GetTowns(state).Select(x => x.City).Distinct().ToList();
                townListDTO.State = state;
                townListDTO.Cities = townList;
            }
            else
            {
                var townList = _repoWrapper.HygeiaHospitalList.GetTowns(state).Select(x => x.City).Distinct().ToList();
                townListDTO.State = state;
                townListDTO.Cities = townList;
            }
            var newList = new List<CityListDTO>
            {
                townListDTO
            };
            return new ResponseMessage { Data = newList, Status = true };
        }

        /// <summary>
        /// Get Health care providers. for Axamansard we use 1 as insuranceProvider params, For Hygeia we use 2 as insuranceprovider params3
        /// </summary>
        /// <param name="state"></param>
        /// <param name="city"></param>
        /// <param name="healthPlan"></param>
        /// <param name="insuranceProvider"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> GetHealthProvider(string state, string city,string insuranceProvider)
        {
            if(insuranceProvider.ToLower() == "axamansard")
            {
                var axaHospitalList = await _repoWrapper.AxaMansardHospitalList.GetHealthProviders(state, city);
                return new ResponseMessage { Data = axaHospitalList, Status = true };
            }
            var hygeiaHospitalList = await _repoWrapper.HygeiaHospitalList.GetHealthProviders(state, city);
            return new ResponseMessage { Data = hygeiaHospitalList, Status = true };
        }

        public async Task<ResponseMessage> FilterHealthCareProvider(PaginationQuery paginationQuery,string state,string city)
        {
            var filterHealthCareProvider = await _repoWrapper.HygeiaHospitalList.FilterHealthCareProvider(paginationQuery, state, city);
            filterHealthCareProvider.PageNumber = paginationQuery.PageNumber >= 1 ? paginationQuery.PageNumber : (int?)null;
            filterHealthCareProvider.PageSize = paginationQuery.PageSize >= 1 ? paginationQuery.PageSize : (int?)null;

            return new ResponseMessage { Data = filterHealthCareProvider, Status = true, Message = "Care provider was fetched successfully" };
        }       

        public async Task<ResponseMessage> CreateCorporateUser(CorporateRegistrationViewModel corporateRegViewModel,int userId)
        {
            var checkIfUserIsRegisteredAsIndividualUser = await _repoWrapper.InsuranceProfile.GetByUserIdAsync(userId);
            if (!(checkIfUserIsRegisteredAsIndividualUser is null)) return new ResponseMessage { Status = false, Message = "User is registered as an individual user" };

            var company = await _repoWrapper.CompanyProfile.GetCompanyProfileByEmail(corporateRegViewModel.Email);

            if(company != null)
            { 
                return new ResponseMessage { Message = "Company profile with this email already exist" };
            }
            var otp = _uniqueIdentifier.GetUniqueCode(6);
            company = new CompanyProfile
            {
                OTPCode = otp,
                UserId = userId,
                CompanyEmail = corporateRegViewModel.Email,
                CompanyName = corporateRegViewModel.Name,
                InsuranceService = InsuranceProvider.Hygeia.ToString()
            };

            // Schedule otp removal after 5 minutes
            var otpJobId = BackgroundJob.Schedule(() => RemoveOTP(userId),DateTime.Now.AddMinutes(5));

            company.OTPJobId = otpJobId;
            _repoWrapper.CompanyProfile.Create(company);
            await _repoWrapper.Save();
            _emailSender.CorporateInsuranceOnboarding(corporateRegViewModel.Email, "Corporating Onboarding", otp);
            return new ResponseMessage { Status = true, Message = "OTP was sent to email successfully" };
        }

        /// <summary>
        /// Task to upload excel list contanning list of beneficiaries under a corporate organization
        /// </summary>
        /// <param name="file"></param>
        /// <param name="companyUserId"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> UploadUserProfileFromExcelFile(IFormFile file,int companyUserId)
        {
            var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(companyUserId);

            if (companyProfile.EmailConfirmed)
            {
                // Process excel list
                var excelModel = await _fileProcessor.ProcessUserProfileFromExcelFile(file);
                // Get company beneficaries from excel list
                var companyBeneficiaries = _mapper.Map<List<FileModel>, List<BeneficiaryReviewUser>>(excelModel);

                var newCompanyBeneficiaries = new List<BeneficiaryReviewUser>();
                // If card is not tokenized we add users in the excel sheet to list of beneficiary Review users
                if (!companyProfile.TokenizationCompleted)
                {
                    foreach (var item in companyBeneficiaries)
                    {
                        var checkIfItemEmailExist = await _repoWrapper.BeneficiaryReview.GetByEmail(item.Email);
                        // Check to make sure beneficiary with the same email cant be uploaded twice
                        if(checkIfItemEmailExist is null)
                        {
                            item.CompanyProfileId = companyProfile.Id;
                            item.Amount = decimal.Parse("1000");
                            newCompanyBeneficiaries.Add(item);
                        }
                    }
                    if(newCompanyBeneficiaries.Count > 0) await _repoWrapper.BeneficiaryReview.InsertEntities(newCompanyBeneficiaries);

                    var beneficiariesReviewDTO = _mapper.Map<List<BeneficiaryReviewUser>, List<BeneficiaryReviewDTO>>(newCompanyBeneficiaries);

                    var paginatedResponse = new PagedResponse<BeneficiaryReviewDTO>
                    {
                        Amount = beneficiariesReviewDTO.Select(x => x.Amount).Sum(),
                        Data = beneficiariesReviewDTO,
                        PageNumber = 1,
                        PageSize = 50,
                        RecordCount = beneficiariesReviewDTO.Count,
                        PageCount = Convert.ToInt32(Math.Ceiling((double)beneficiariesReviewDTO.Count / (double)50))
                    };
                    return new ResponseMessage
                    {
                        Data = paginatedResponse,
                        Message = "Uploaded Successfully",
                        Status = true
                    };
                }
                // Else we process users in the excel sheet to be added to the Beneficaries List either as pending or active beneficiairy
                return  await CreateInsuranceProfileForCompanyBeneficiaries(companyProfile.Id, companyBeneficiaries);
            } 
            return new ResponseMessage
            {
                Message = "Please confirm company email address",
                Status = false
            };
        }

        /// <summary>
        /// This task adds the beneficiary as an application and insurance users. Also process maybe to set the user as a pending beneficiary or active beneficiary.
        /// If beneficaryReviews is null, user would be set as active beneficiaries, else users will be a  pending beneficiary.
        /// </summary>
        /// <param name="companyId"></param>
        /// <param name="beneficiaryReviews"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> CreateInsuranceProfileForCompanyBeneficiaries(int companyId, List<BeneficiaryReviewUser> beneficiaryReviews)
        {
            var companySubscribedStatus = InsuranceProfile_CompanySubStatusValue.Pending.ToString();
            var companyprofile = await _repoWrapper.CompanyProfile.GetCompanyBeneficiaryReviewUsersByCompanyId(companyId);

            if (beneficiaryReviews is null){
                beneficiaryReviews = companyprofile.BeneficiaryReviewUsers.Where(x => x.IsRemove == false).ToList();
                companySubscribedStatus = InsuranceProfile_CompanySubStatusValue.Active.ToString();
            }
            var insuranceUserProfiles = new List<InsuranceUserProfile>();
            foreach (var item in beneficiaryReviews)
            {
                var user = _mapper.Map<ApplicationUser>(item);
                var createdUser = await _identityService.RegisterUserWithoutPassword(user);
                if (createdUser.Status)
                {
                    var userId = createdUser.Data.Id;
                    var insuranceUserProfile = _mapper.Map<InsuranceUserProfile>(item);
                    insuranceUserProfile.CompanyProfileId = companyId;
                    insuranceUserProfile.InsuranceService = InsuranceProvider.Hygeia.ToString();
                    insuranceUserProfile.Premium = Decimal.Parse("1000");
                    insuranceUserProfile.CompanySubscribedStatus = companySubscribedStatus;
                    insuranceUserProfile.UserId = userId;
                    if(companySubscribedStatus == InsuranceProfile_CompanySubStatusValue.Active.ToString())
                    {
                        insuranceUserProfile.ActiveStatus = true;
                        insuranceUserProfile.SubscriptionStatus = true;
                        insuranceUserProfile.StartActiveStatusDate = DateTime.Now;
                        insuranceUserProfile.EndActiveStatusDate = DateTime.Now.AddDays(_subscriptionAccessor.FreeTrialDayDuration);
                    }
                    insuranceUserProfiles.Add(insuranceUserProfile);

                    companyprofile.NextCyclePremiumFee += insuranceUserProfile.Premium;
                }
            }
            _repoWrapper.CompanyProfile.Update(companyprofile);
            _repoWrapper.InsuranceProfile.CreateRange(insuranceUserProfiles);
            var beneficiaries = companyprofile.BeneficiaryReviewUsers.ToList();
            if(beneficiaries.Count > 0)
            {
                _repoWrapper.BeneficiaryReview.DeleteRange(beneficiaries);
            }
            var activityLog = new ActivityLog(null, companyprofile.Id, "New Beneficiairies Was Added", ServiceNames.HealthInsured.ToString());
            _repoWrapper.ActivityLog.Create(activityLog);

            await _repoWrapper.Save();
            return new ResponseMessage { Message = "Profiles was created successfully", Status=true };
        }

        public async Task OnboardUsersToHygeia(int companyUserId)
        {
            var companyProfile = await _repoWrapper.InsuranceProfile.QueryableInsuranceProfilesUnderCompany(companyUserId);
            var insuranceUserProfiles = companyProfile.Where(x => x.CompanySubscribedStatus == InsuranceProfile_CompanySubStatusValue.Pending.ToString());
            foreach (var item in insuranceUserProfiles)
            {
                var result = await EnrollUserToHygeiaOnOnboarding(item);
                item.TransId = result.Message;
                _repoWrapper.InsuranceProfile.Update(item);
            }
            await Task.CompletedTask;
        }

        public async Task<ResponseMessage> ConfirmOtp(string otp,int userId)
        {
            var corporateUser = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(userId);

            if (corporateUser != null)
            {
                if (corporateUser.EmailConfirmed)
                {
                    return new ResponseMessage { Message = "Email was confirmed previously", Status = false };
                }
                if (corporateUser.OTPCode != null)
                {
                    if (otp == corporateUser.OTPCode)
                    {
                        corporateUser.EmailConfirmed = true;
                        corporateUser.OTPCode = null;
                        BackgroundJob.Delete(corporateUser.OTPJobId);
                        corporateUser.OTPJobId = null;
                        _repoWrapper.CompanyProfile.Update(corporateUser);
                        await _repoWrapper.Save();
                        return new ResponseMessage { Message = "Email was confirmed successfully", Status = true };
                    }
                    return new ResponseMessage { Message = "OTP code does not match", Status = false };
                }
                return new ResponseMessage { Message = "OTP code has expired, please resend OTP", Status = false };
            }
            return new ResponseMessage { Message = "Company profile does not exist", Status = false };
        }
        
        public async Task<ResponseMessage> ResendOtp(int userId)
        {
            var company = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(userId);
            if (company == null)
            {
                return new ResponseMessage { Message = "Company profile does not exist, please register as a corporate entity.",Status=false };
            }
            else if (company.EmailConfirmed)
            {
                return new ResponseMessage { Message = "Email was confirmed previously", Status = false };
            }
            var otp = _uniqueIdentifier.GetUniqueCode(6);
            company.OTPCode = otp;
            if(company.OTPJobId != null)
            {
                BackgroundJob.Delete(company.OTPJobId);
            }
            // Schedule otp removal after 5 minutes
            var otpJobId = BackgroundJob.Schedule(() => RemoveOTP(userId), DateTime.Now.AddMinutes(5));

            company.OTPJobId = otpJobId;
            _repoWrapper.CompanyProfile.Update(company);
            await _repoWrapper.Save();
            _emailSender.CorporateInsuranceOnboarding(company.CompanyEmail, "Corporating Onboarding", otp);
            return new ResponseMessage { Message = "OTP was sent successfully", Status = true };
        }

        public async Task RemoveOTP(int userId)
        {
            var corporateUser = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(userId);

            if (!(corporateUser is null))
            {
                corporateUser.OTPCode = null;
                corporateUser.OTPJobId = null;
                _repoWrapper.CompanyProfile.Update(corporateUser);
                await _repoWrapper.Save();
            }
            await Task.CompletedTask;
        }

        public async Task<ResponseMessage> UpdateCorporateUser(UpdateCorporateUserViewModel updateCorporateUserViewModel,int Id)
        {
            var company = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(Id);
            if(company is null)
            {
                return new ResponseMessage { Message = "Company does not exist", Status = false };
            }
            company.Industry = updateCorporateUserViewModel.Industry;
            company.CompanySize = updateCorporateUserViewModel.CompanySize;
            company.ProfileCompleted = true;
            _repoWrapper.CompanyProfile.Update(company);

            var activityLog = new ActivityLog(null, company.Id, "Company Profile Was Updated", ServiceNames.HealthInsured.ToString());
            _repoWrapper.ActivityLog.Create(activityLog);

            await _repoWrapper.Save();
            var corporateprofileDTO = _mapper.Map<CompanyProfileDTO>(company);
            return new ResponseMessage { Message = "Company profile was updated successfully", Status = true,Data= corporateprofileDTO };
        }        

        public async Task<ResponseMessage> GetBeneficiariesReview(PaginationQuery paginationQuery,int companyUserId)
        {
            var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(companyUserId);

            var companyBeneficiaries = await _repoWrapper.BeneficiaryReview.FilterBeneficiariesReview(paginationQuery, companyProfile.Id);
            var beneficiariesReviewDTO = _mapper.Map<IEnumerable<BeneficiaryReviewUser>, IEnumerable<BeneficiaryReviewDTO>>(companyBeneficiaries.Data);

            var paginatedResponse = new PagedResponse<BeneficiaryReviewDTO>
            {
                Data = beneficiariesReviewDTO,
                Amount = beneficiariesReviewDTO.Select(x => x.Amount).Sum(),
                PageNumber = paginationQuery.PageNumber >= 1 ? paginationQuery.PageNumber : (int?)null,
                PageSize = paginationQuery.PageSize >= 1 ? paginationQuery.PageSize : (int?)null,
                RecordCount = companyBeneficiaries.RecordCount,
                PageCount = companyBeneficiaries.PageCount
            };
            return new ResponseMessage { Data = paginatedResponse, Status = true, Message = "Beneficiaries was fetched successfully" };
        }

        public async Task<ResponseMessage> GetCompanyBeneficiaries(PaginationQuery paginationQuery,int companyUserId)
        {
            var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(companyUserId);

            var beneficiaries = await _repoWrapper.InsuranceProfile.GetAllInsuranceProfileUnderCompany(paginationQuery, companyProfile.Id);
            var beneficiariesDTO = _mapper.Map<IEnumerable<InsuranceUserProfile>, List<InsuranceBeneficiaryDTO>>(beneficiaries.Data);

            var paginatedResponse = new PagedResponse<InsuranceBeneficiaryDTO>
            {
                Data = beneficiariesDTO,
                Amount = beneficiariesDTO.Select(x => x.Amount).Sum(),
                PageNumber = paginationQuery.PageNumber >= 1 ? paginationQuery.PageNumber : (int?)null,
                PageSize = paginationQuery.PageSize >= 1 ? paginationQuery.PageSize : (int?)null,
                RecordCount = beneficiaries.RecordCount,
                PageCount = beneficiaries.PageCount
            };
            return new ResponseMessage { Data = paginatedResponse, Status = true, Message = "Beneficiaries was fetched successfully" };
        }

        public async Task<ResponseMessage> MoveCompanyBeneficiaryFromInactiveToPendingState(BeneficiaryListViewModel beneficiaryListViewModel, int userId)
        {
            var companyprofile = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(userId);
            foreach(var item in beneficiaryListViewModel.Emails)
            {
                var insuranceUserProfile = await _repoWrapper.InsuranceProfile.GetByEmail(item);
                if(insuranceUserProfile != null && insuranceUserProfile.CompanySubscribedStatus == InsuranceProfile_CompanySubStatusValue.Inactive.ToString())
                {
                    insuranceUserProfile.CompanySubscribedStatus = InsuranceProfile_CompanySubStatusValue.Pending.ToString();
                    companyprofile.NextCyclePremiumFee += insuranceUserProfile.Premium;
                    _repoWrapper.InsuranceProfile.Update(insuranceUserProfile);
                    _repoWrapper.CompanyProfile.Update(companyprofile);
                }
            }
            await _repoWrapper.Save();
            return new ResponseMessage { Status = true };
        }

        public async Task<ResponseMessage> CorporateSubscribersAnalytics(int companyUserId)
        {
            var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyInsuranceUserProfilesByCompanyId(companyUserId);

            var activeBeneficiaryCount = companyProfile.InsuranceUserProfiles.Where(x => x.ActiveStatus == true).Count();
            var pendingbeneficiarycount = companyProfile.InsuranceUserProfiles.Where(x => x.CompanySubscribedStatus == InsuranceProfile_CompanySubStatusValue.Pending.ToString()).Count();
            var inactiveBeneficairyCount = companyProfile.InsuranceUserProfiles.Where(x => x.ActiveStatus == false).Count();

            var companyProfileBeneficiaryAnalyticDTO = new CompanyProfileBeneficiaryAnalyticDTO
            {
                ActiveBeneficiaryCount = activeBeneficiaryCount,
                InactiveBeneficiaryCount = inactiveBeneficairyCount,
                PendingBeneficiaryCount = pendingbeneficiarycount,
                NextPaymentDate = companyProfile.NextPaymentDate
            };
            return new ResponseMessage { Status = true, Data = companyProfileBeneficiaryAnalyticDTO, Message = "Analytics was fetched successfully" };
        }

        public async Task<ResponseMessage> GetCompanyProfileDetails(int companyUserId)
        {
            var company = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(companyUserId);

            var companyProfileDTO = _mapper.Map<CompanyProfileDTO>(company);

            return new ResponseMessage { Data = companyProfileDTO, Status = true, Message = "Company profile was fetched successfully" };
        }

        public async Task<ResponseMessage> RemoveCompanyBeneficiary(string email,int companyUserId)
        {
            var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(companyUserId);
            var beneficiary = await _repoWrapper.BeneficiaryReview.GetByEmail(email);
            if(beneficiary.CompanyProfileId == companyProfile.Id)
            {
                beneficiary.IsRemove = true;
                _repoWrapper.BeneficiaryReview.Update(beneficiary);

                await _repoWrapper.Save();

                return new ResponseMessage { Status = true, Message = "Status was changed successfully" };
            }
            return new ResponseMessage { Status = false, Message = "You cannot change beneficiary status" };
        }

        public async Task<ResponseMessage> RestoreCompanyBeneficiary(string email, int companyUserId)
        {
            var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(companyUserId);
            var beneficiary = await _repoWrapper.BeneficiaryReview.GetByEmail(email);
            if (beneficiary.CompanyProfileId == companyProfile.Id)
            {
                beneficiary.IsRemove = false;
                _repoWrapper.BeneficiaryReview.Update(beneficiary);
                await _repoWrapper.Save();
                return new ResponseMessage { Status = true, Message = "Status was changed successfully" };
            }
            return new ResponseMessage { Status = false, Message = "You cannot change beneficiary status" };
        }        

        public async Task<ResponseMessage> UploadAxaHospitalListFromExcel(IFormFile formFile)
        {
            var hospitalList = await _fileProcessor.UploadAxaHospitalListFromExcel(formFile);
            _repoWrapper.AxaMansardHospitalList.CreateRange(hospitalList);
            await _repoWrapper.Save();
            return new ResponseMessage { Status = true, Message = "Upload was successful" };
        }

        public async Task<ResponseMessage> UploadHygeiaHospitalListFromExcel(IFormFile formFile)
        {
            var hospitalList = await _fileProcessor.UploadHygeiaHospitalListFromExcel(formFile);
            _repoWrapper.HygeiaHospitalList.CreateRange(hospitalList);
            await _repoWrapper.Save();
            return new ResponseMessage { Status = true, Message = "Upload was successful" };
        }
    }
}
