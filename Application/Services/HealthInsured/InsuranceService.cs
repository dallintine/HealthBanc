using Application.DTO;
using Application.AuditAndReport.AuditLog;
using Application.Helpers;
using Application.ViewModels;
using Application.ViewModels.HealthInsured;
using AutoMapper;
using DataAccess.General.Interfaces;
using DataAccess.HealthInsured.Interfaces;
using Domain.Models;
using Domain.Models.AxaMansard_Insurance;
using Hangfire;
using HealthBanc.DTO.HealthInsured_AxaMansard;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Application.Interfaces;
using Application.API_RequestModel.HealthInsured;
using Application.API_ResponseModel.HealthInsured;
using DataAccess.HealthInsured_AxaMansard.Interfaces;
using Domain.Models.Axa.Hygeia_Insurance;
using Microsoft.AspNetCore.Identity;
using Application.Services.Identity;
using DataAccess;
using Application.DTO.HealthInsured_AxaMansard;

namespace Application.Services.HealthInsured
{
    public class InsuranceService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IApplicationUserRepository _userRepository;
        private readonly IInsuranceProfileRepository _insuranceProfileRepository;
        private readonly IMapper _mapper;
        private readonly IInsuranceCompletionProfileRepository _completionRepository;
        private readonly AuditLogService _auditLogServices;
        private readonly ExcelPackage _excelPackage;
        private readonly IAxaMansardHospitalListRepository _hospitalListRepository;
        private readonly IHygeiaHospitalListRepository _hygeiaHospitalListRepository;
        private readonly ILogger<InsuranceService> _logger;
        private readonly IUniqueIdentifier _uniqueIdentifier;
        private readonly ICompanyProfileRepository _companyProfileRepository;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFileProcessor _fileProcessor;
        private readonly IdentityService _identityService;
        private readonly ICompanyInsuranceUserRepository _companyInsuranceUserRepository;

        private AxaMansardConfiguration Options { get; }
        private HygeiaConfiguration _hygeiaAccessor { get; }

        public InsuranceService(IHttpClientFactory httpClientFactory, IOptions<AxaMansardConfiguration> axaAccessor, IApplicationUserRepository userRepository
            , IInsuranceProfileRepository insurance, IMapper mapper, IInsuranceCompletionProfileRepository completionRepository, AuditLogService auditLogServices,
            ExcelPackage excelPackage, IAxaMansardHospitalListRepository hospitalListRepository, ILogger<InsuranceService> logger, IUniqueIdentifier uniqueIdentifier,
             IOptions<HygeiaConfiguration> hygeiaAccessor, ICompanyProfileRepository companyProfileRepository, IEmailSender emailSender,
              UserManager<ApplicationUser> userManager, IFileProcessor fileProcessor, IdentityService identityService
            , ICompanyInsuranceUserRepository companyInsuranceUserRepository, IHygeiaHospitalListRepository hygeiaHospitalListRepository)
        {
            _httpClientFactory = httpClientFactory;
            _userRepository = userRepository;
            _insuranceProfileRepository = insurance;
            _mapper = mapper;
            _completionRepository = completionRepository;
            _auditLogServices = auditLogServices;
            _excelPackage = excelPackage;
            _hospitalListRepository = hospitalListRepository;
            _logger = logger;
            _uniqueIdentifier = uniqueIdentifier;
            _companyProfileRepository = companyProfileRepository;
            _emailSender = emailSender;
            _userManager = userManager;
            _fileProcessor = fileProcessor;
            _identityService = identityService;
            _companyInsuranceUserRepository = companyInsuranceUserRepository;
            Options = axaAccessor.Value;
            _hygeiaAccessor = hygeiaAccessor.Value;
            _hygeiaHospitalListRepository = hygeiaHospitalListRepository;
        }

        public async Task<ResponseMessage> UserOnboarding(UserProfileviewModel userProfile, int userId,string ipAddress,string device)
        {
            var user = await _userRepository.FindByIdAsync(userId);

            var checkIfUserHasBeenProfiled = await _insuranceProfileRepository.GetByUserIdAsync(userId);
            if (checkIfUserHasBeenProfiled != null) return new ResponseMessage { Message = "User has a profile already" };


            var supportedTypes = new[] { ".JPG", ".JPE", ".PNG", ".JPEG" };
            var customerPhoto = System.IO.Path.GetExtension(userProfile.CustomerPhoto.FileName).ToUpperInvariant();
            var identityPhoto = System.IO.Path.GetExtension(userProfile.IdentityPhoto.FileName).ToUpperInvariant();

            if (!supportedTypes.Contains(customerPhoto) || !supportedTypes.Contains(identityPhoto))
            {
                return new ResponseMessage { Message = "FIle extension is invalid - Only Upload PNG/JPEG/JPG/JPE" };
            }

            var customerPhotorSize = userProfile.CustomerPhoto.Length;
            var identityPhotoSize = userProfile.IdentityPhoto.Length;
            if ((customerPhotorSize / 1048576) > 4 || (identityPhotoSize / 1048576) > 4)
            {
                return new ResponseMessage { Message = "Image size is too large - Size should be less than four Megabyte" };
            }

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
            userProfile.InsuranceService = userProfile.InsuranceService == null ? userProfile.InsuranceService = "axamansard"
               : userProfile.InsuranceService = userProfile.InsuranceService;

            var profile = _mapper.Map<InsuranceUserProfile>(userProfile);
            profile.UserId = user.Id;

            profile.CareProviderName = userProfile.CareProviderName.Split(":")[0]; profile.CPAddress = userProfile.CareProviderName.Split(":")[1];

            profile.TransId = (userProfile.InsuranceService == null | userProfile.InsuranceService == "axamansard") ? _uniqueIdentifier.GetUniqueCode(12) : "";
            profile.CPCity = userProfile.CareProviderName.Split(":").Length == 3 ? userProfile.CareProviderName.Split(":")[2] : "";
            profile.InsuranceService = userProfile.InsuranceService;

            var updatedProfile = _mapper.Map(user, profile);

            _insuranceProfileRepository.Create(updatedProfile);

            var completionProfile = new InsuranceCompletionProfile(user.Id, true, false,userProfile.InsuranceService);
            _completionRepository.Create(completionProfile);
            await _completionRepository.Save();

            var newServiceString = user.ServiceUsed + "HealthInsured,";
            user.ServiceUsed = newServiceString;
            _userRepository.Update(user);
            await _userRepository.Save();
            return new ResponseMessage { Status = true};
        }

        public async Task<ResponseMessage> AxamansardRegisterUser(EnrollmentModel model)
        {
            try
            {
                model.EntityCode = Options.EntityCode;
                var bearerRequest = await AxaMansardAuthentication();
                if (bearerRequest.Status)
                {
                    var httpClient = _httpClientFactory.CreateClient("AxaMansard");
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerRequest.Data.Auth_token);
                    HttpContent content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync($"{Options.AxaMansardEnrollement}", content);

                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        var authResponse = JsonConvert.DeserializeObject<EnrollementResponse>(apiResponse);
                        //if (authResponse.success)
                        //{
                        //    return new ResponseMessage { Status = true, Message = authResponse.message };
                        //}
                        return new ResponseMessage { Status = true, Message = authResponse.message };
                    }
                    return new ResponseMessage { Status = false, Message = "Could not connect to insurance provider. Please try again later" };
                }
                return new ResponseMessage { Status = false, Message = bearerRequest.Message };
            }
            catch (Exception ex)
            {
                _logger.LogCritical("An error occurred while enrolling user to axa-mansard", ex);
                return new ResponseMessage { Status = false, Message = "This on us.An error occurred while enrolling user to axa-mansard.Please try again later" };
            }
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
                return new ResponseMessage { Status = false, Message = "This on us.An error occurred while enrolling user to axa-mansard.Please try again later" };
            }
        }

        public async Task<ResponseMessage> HygeiaDeactivateUser(string enrollNumber)
        {
            var httpClient = _httpClientFactory.CreateClient("Hygeia");
            var response = await httpClient.PutAsync($"{_hygeiaAccessor.HygeiaDeactivate}/{enrollNumber}", null);

            var deactivateResponse = new HygeiaDeactivateResponse();
            string apiResponse = await response.Content.ReadAsStringAsync();
            deactivateResponse = JsonConvert.DeserializeObject<HygeiaDeactivateResponse>(apiResponse);
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

        public async Task<ResponseMessage<HealthInsuredProfileStateDTO>> GetProfileCompletion(int userId)
        {
            var profile = await _completionRepository.GetCompletionStateByUserId(userId);
            if (profile == null)
            {
                var corporateUser = await _companyProfileRepository.GetCompanyProfileByUserId(userId);
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
                var axaHospitalList = await _hospitalListRepository.GetHealthProviders(state, city);
                return new ResponseMessage { Data = axaHospitalList, Status = true };
            }
            var hygeiaHospitalList = await _hygeiaHospitalListRepository.GetHealthProviders(state, city);
            return new ResponseMessage { Data = hygeiaHospitalList, Status = true };
        }

        public async Task<ResponseMessage> FilterHealthCareProvider(PaginationQuery paginationQuery,string state,string city)
        {
            var filterHealthCareProvider = await _hygeiaHospitalListRepository.FilterHealthCareProvider(paginationQuery, state, city);
            filterHealthCareProvider.PageNumber = paginationQuery.PageNumber >= 1 ? paginationQuery.PageNumber : (int?)null;
            filterHealthCareProvider.PageSize = paginationQuery.PageSize >= 1 ? paginationQuery.PageSize : (int?)null;

            return new ResponseMessage { Data = filterHealthCareProvider, Status = true, Message = "Care provider was fetched successfully" };
        }

        public ResponseMessage GetTowns(string state,string insuranceProvider)
        {
            var townListDTO = new CityListDTO();
            if (insuranceProvider.ToLowerInvariant() == "axamansard")
            {
                var townList = _hospitalListRepository.GetTowns(state).Select(x => x.City).Distinct().ToList();
                townListDTO.State = state;
                townListDTO.Cities = townList;
            }
            else
            {
                var townList = _hygeiaHospitalListRepository.GetTowns(state).Select(x => x.City).Distinct().ToList();
                townListDTO.State = state;
                townListDTO.Cities = townList;
            }
            var newList = new List<CityListDTO>();
            newList.Add(townListDTO);
            return new ResponseMessage { Data = newList, Status = true };
        }

        public async Task<ResponseMessage> UploadAxaHospitalListFromExcel(IFormFile formFile)
        {
            var hospitalList = await _fileProcessor.UploadAxaHospitalListFromExcel(formFile);
            _hospitalListRepository.CreateRange(hospitalList);
            await _hospitalListRepository.Save();
            return new ResponseMessage { Status = true, Message = "Upload was successful" };
        }

        public async Task<ResponseMessage> UploadHygeiaHospitalListFromExcel(IFormFile formFile)
        {
            var hospitalList = await _fileProcessor.UploadHygeiaHospitalListFromExcel(formFile);
            _hygeiaHospitalListRepository.CreateRange(hospitalList);
            await _hospitalListRepository.Save();
            return new ResponseMessage { Status = true, Message = "Upload was successful" };
        }

        public async Task<ResponseMessage> CreateCorporateUser(CorporateRegistrationViewModel corporateRegViewModel,int userId)
        {
            var company = await _companyProfileRepository.GetCompanyProfileByEmail(corporateRegViewModel.Email);
            if(company != null)
            { 
                return new ResponseMessage { Message = "Company profile with this email already exist" };
            }
            var otp = _uniqueIdentifier.GetUniqueCode(6);
            company = new CompanyProfile();
            company.OTPCode = otp;
            company.UserId = userId;
            company.CompanyEmail = corporateRegViewModel.Email;
            company.CompanyName = corporateRegViewModel.Name;
            company.InsuranceService = "Hygeia";

            // Schedule otp removal after 5 minutes
            var otpJobId = BackgroundJob.Schedule(() => RemoveOTP(userId),DateTime.Now.AddMinutes(5));

            company.OTPJobId = otpJobId;
            _companyProfileRepository.Create(company);
            await _companyProfileRepository.Save();
            _emailSender.CorporateInsuranceOnboarding(corporateRegViewModel.Email, "Corporating Onboarding", otp);
            return new ResponseMessage { Status = true, Message = "OTP was sent to email successfully" };
        }

        public async Task<ResponseMessage> UploadUserProfileFromExcelFile(IFormFile file,int companyUserId)
        {
            var companyProfile = await _companyProfileRepository.GetCompanyProfileByUserId(companyUserId);

            if (companyProfile.EmailConfirmed)
            {
                var excelModel = await _fileProcessor.ProcessUserProfileFromExcelFile(file);

                var companyInsuranceUsers = _mapper.Map<List<FileModel>, List<CompanyInsuranceUser>>(excelModel);
                foreach(var item in companyInsuranceUsers)
                {
                    item.CompanyProfileId = companyProfile.Id;
                }
                await _companyInsuranceUserRepository.InsertEntities(companyInsuranceUsers);

                var paginatedResponse = new PagedResponse<CompanyInsuranceUser>
                {
                    Data = companyInsuranceUsers,
                    PageNumber = 1,
                    PageSize = 50,
                    RecordCount = companyInsuranceUsers.Count,
                    PageCount = Convert.ToInt32(Math.Ceiling((double)companyInsuranceUsers.Count / (double)50))
                };
                return new ResponseMessage
                {
                    Data = paginatedResponse,
                    Message = "Uploaded Successfully",
                    Status = true
                };
            };            
            return new ResponseMessage
            {
                Message = "Please confirm company email address",
                Status = false
            };
        }

        public async Task<ResponseMessage> ConfirmOtp(string otp,int userId)
        {
            var corporateUser = await _companyProfileRepository.GetCompanyProfileByUserId(userId);

            if (corporateUser.OTPCode != null)
            {
                if (corporateUser.EmailConfirmed)
                {
                    return new ResponseMessage { Message = "Email was confirmed previously", Status = false };
                }
                if (otp == corporateUser.OTPCode)
                {
                    corporateUser.EmailConfirmed = true;
                    corporateUser.OTPCode = null;
                    BackgroundJob.Delete(corporateUser.OTPJobId);
                    corporateUser.OTPJobId = null;
                    _companyProfileRepository.Update(corporateUser);
                    await _companyProfileRepository.Save();
                    return new ResponseMessage { Message = "Email was confirmed successfully", Status = true };
                }
                return new ResponseMessage { Message = "OTP code does not match", Status = false };
            }
            return new ResponseMessage { Message = "OTP code has expired, please resend OTP", Status = false };
        }
         
        public async Task<ResponseMessage> ResendOtp(int userId)
        {
            var company = await _companyProfileRepository.GetCompanyProfileByUserId(userId);
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
            _companyProfileRepository.Update(company);
            await _companyProfileRepository.Save();
            _emailSender.CorporateInsuranceOnboarding(company.CompanyEmail, "Corporating Onboarding", otp);
            return new ResponseMessage { Message = "OTP was sent successfully", Status = true };
        }

        public async Task<ResponseMessage> UpdateCorporateUser(UpdateCorporateUserViewModel updateCorporateUserViewModel,int Id)
        {
            var company = await _companyProfileRepository.GetCompanyProfileByUserId(Id);
            if(company is null)
            {
                return new ResponseMessage { Message = "Company does not exist", Status = false };
            }
            company.Industry = updateCorporateUserViewModel.Industry;
            company.CompanySize = updateCorporateUserViewModel.CompanySize;
            company.ProfileCompleted = true;
            _companyProfileRepository.Update(company);
            await _companyProfileRepository.Save();
            var corporateprofileDTO = _mapper.Map<CompanyProfileDTO>(company);
            return new ResponseMessage { Message = "Company profile was updated successfully", Status = true,Data= corporateprofileDTO };
        }        

        public async Task RemoveOTP(int userId)
        {
            var corporateUser = await _companyProfileRepository.GetCompanyProfileByUserId(userId);
            corporateUser.OTPCode = null;
            corporateUser.OTPJobId = null;
            _companyProfileRepository.Update(corporateUser);
            await _companyProfileRepository.Save();
        }

        public async Task<ResponseMessage> GetCompanyBeneficiaries(PaginationQuery paginationQuery,int companyUserId)
        {
            var companyProfile = await _companyProfileRepository.GetCompanyProfileByUserId(companyUserId);

            var beneficiaries = await _insuranceProfileRepository.GetAllInsuranceProfileUnderCompany(paginationQuery, companyProfile.Id);
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

        public async Task<ResponseMessage> CorporateSubscribersAnalytics(int companyUserId)
        {
            var companyProfile = await _insuranceProfileRepository.QueryableCompanyProfile(companyUserId);

            var activeBeneficiaryCount = companyProfile.Where(x => x.ActiveStatus == true).Count();
            var inactiveBeneficairyCount = companyProfile.Where(x => x.SubscriptionStatus == false && x.ActiveStatus == false).Count();

            var companyProfileBeneficiaryAnalyticDTO = new CompanyProfileBeneficiaryAnalyticDTO
            {
                ActiveBeneficiaryCount = activeBeneficiaryCount,
                InactiveBeneficiaryCount = inactiveBeneficairyCount,
                NextPaymentDate = DateTime.Now
            };
            return new ResponseMessage { Status = true, Data = companyProfileBeneficiaryAnalyticDTO, Message = "Analytics was fetched successfully" };
        }

        public async Task<ResponseMessage> GetCompanyProfileDetails(int companyUserId)
        {
            var company = await _companyProfileRepository.GetCompanyProfileByUserId(companyUserId);

            var companyProfileDTO = _mapper.Map<CompanyProfileDTO>(company);

            return new ResponseMessage { Data = companyProfileDTO, Status = true, Message = "Company profile was fetched successfully" };
        }

        public async Task<ResponseMessage> RemoveCompanyBeneficiary(string email,int companyUserId)
        {
            var companyProfile = await _companyProfileRepository.GetCompanyProfileByUserId(companyUserId);
            var beneficiary = await _companyInsuranceUserRepository.GetByEmail(email);
            if(beneficiary.CompanyProfileId == companyProfile.Id)
            {
                beneficiary.InActiveStatus = true;
                _companyInsuranceUserRepository.Update(beneficiary);
                await _companyInsuranceUserRepository.Save();
                return new ResponseMessage { Status = true, Message = "Status was changed successfully" };
            }
            return new ResponseMessage { Status = false, Message = "You cannot change beneficiary status" };
        }

        public async Task<ResponseMessage> RestoreCompanyBeneficiary(string email, int companyUserId)
        {
            var companyProfile = await _companyProfileRepository.GetCompanyProfileByUserId(companyUserId);
            var beneficiary = await _companyInsuranceUserRepository.GetByEmail(email);
            if (beneficiary.CompanyProfileId == companyProfile.Id)
            {
                beneficiary.InActiveStatus = false;
                _companyInsuranceUserRepository.Update(beneficiary);
                await _companyInsuranceUserRepository.Save();
                return new ResponseMessage { Status = true, Message = "Status was changed successfully" };
            }
            return new ResponseMessage { Status = false, Message = "You cannot change beneficiary status" };
        }

        //public async Task<ResponseMessage> ChangeCompanyRegisterdUserStatus(int companyUserId,int companySubscriberUserId,int status) 
        //{
        //    var companyProfile = await _companyProfileRepository.GetCompanyProfileByUserId(companyUserId);
        //    var companySubscriber = await _insuranceProfileRepository.GetByUserIdAsync(companySubscriberUserId);
        //    if(companySubscriber.CompanyProfileId == companyProfile.Id)
        //    {
        //        // Change Status to Inactive
        //        if(status == 1)
        //        {
        //            if((companySubscriber.ActiveStatus == false || companySubscriber.ActiveStatus == null) && companySubscriber.CompanySubscribedStatus == "pending")
        //            {
        //                companySubscriber.CompanySubscribedStatus = "inactive";
        //                return new ResponseMessage { Status = false, Message = "Status cannot be change to Inactive" };
        //            }
        //                return new ResponseMessage { Status = false, Message = "Status cannot be change to Inactive" };
        //        }
        //        // Change Status to Pending
        //        else if(status == 2)
        //        {

        //        }
        //    }
        //}
    }
}
