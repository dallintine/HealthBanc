using Application.API_RequestModel.HealthInsured;
using Application.API_ResponseModel.HealthInsured;
using Application.AuditAndReport.AuditLog;
using Application.DTO;
using Application.DTO.HealthInsured_AxaMansard;
using Application.Helpers;
using Application.Interfaces;
using Application.Services.HealthInsured;
using Application.Services.Identity;
using Application.ViewModels;
using Application.ViewModels.HealthInsured;
using AutoMapper;
using ClosedXML.Excel;
using DataAccess;
using Domain.Models;
using Domain.Models.Axa.Hygeia_Insurance;
using Domain.Models.Axa_Hygeia_Insurance;
using Domain.Models.ReportAndLogs;
using Hangfire;
using Hangfire.Server;
using HealthBanc.DTO.HealthInsured_AxaMansard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Application.HealthInsured_AxaMansard_Service.Insurance
{
    public class InsuranceService
    {
        private readonly IMapper _mapper;
        private readonly AuditLogService _auditLogServices;
        private readonly ILogger<InsuranceService> _logger;
        private readonly IUniqueIdentifier _uniqueIdentifier;
        private readonly IEmailSender _emailSender;
        private readonly IFileProcessor _fileProcessor;
        private readonly IRepositoryWrapper _repoWrapper;
        private readonly HMOIntegrationService _hmoIntegrationService;
        private readonly IImageService _imageService;
        public UserManager<ApplicationUser> UserManager { get; }

        public InsuranceService(IMapper mapper, AuditLogService auditLogServices,ILogger<InsuranceService> logger, IUniqueIdentifier uniqueIdentifier, IEmailSender emailSender,
            IFileProcessor fileProcessor, IRepositoryWrapper repoWrapper, UserManager<ApplicationUser> userManager,
            HMOIntegrationService hmoIntegrationService,IImageService imageService)
        {
            _mapper = mapper;
            _auditLogServices = auditLogServices;
            _logger = logger;
            _uniqueIdentifier = uniqueIdentifier;
            _emailSender = emailSender;
            _fileProcessor = fileProcessor;
            _repoWrapper = repoWrapper;
            UserManager = userManager;
            _hmoIntegrationService = hmoIntegrationService;
            _imageService = imageService;
        }

        public async Task<ResponseMessage> UserOnboarding(UserProfileviewModel userProfile, int userId,string ipAddress,string device)
        {
            var user = await _repoWrapper.ApplicationUser.FindByIdAsync(userId);

            var checkIfUserHasBeenProfiled = await _repoWrapper.InsuranceProfile.GetByUserIdAsync(userId);
            if (checkIfUserHasBeenProfiled != null) return new ResponseMessage { Message = "User has a profile already" };

            var validImageExtension = new [] { ".JPG",".JPEG", ".JPE", ".BMP", ".GIF", ".PNG" };

            var fileExtension = System.IO.Path.GetExtension(userProfile.UserImage.FileName.ToUpper());
            if (!validImageExtension.Contains(fileExtension))
            {
                return new ResponseMessage { Message = "Image type is not supported - Only upload PNG/JPEG/JPG/JPE" };
            }

            var fileSize = userProfile.UserImage.Length;
            if((fileSize/1048576) > 2.1)
            {
                return new ResponseMessage { Message = "Image Size is too large - Size should be less than 2MB" };
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
            var checkIfUserIsRegisteredAsCorporateUser = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(user.Id);
            if (!(checkIfUserIsRegisteredAsCorporateUser is null)) return new ResponseMessage { Status = false, Message = "Üser is registered as corporate user" };

            userProfile.InsuranceService ??= InsuranceProvider.Axamansard.ToString();

            var profile = _mapper.Map<InsuranceUserProfile>(userProfile);
            profile.UserId = user.Id;

            profile.CareProviderName = userProfile.CareProviderName.Split(":")[0]; profile.CPAddress = userProfile.CareProviderName.Split(":")[1];

            profile.TransId = (userProfile.InsuranceService == null | userProfile.InsuranceService == InsuranceProvider.Axamansard.ToString()) ? _uniqueIdentifier.GetUniqueCode(10) : "";
            profile.CPCity = userProfile.CareProviderName.Split(":").Length == 3 ? userProfile.CareProviderName.Split(":")[2] : "";
            profile.InsuranceService = userProfile.InsuranceService;

            var updatedProfile = _mapper.Map(user, profile);
            updatedProfile.Image = _imageService.ConvertImageToBase64(userProfile.UserImage);

            if(updatedProfile.Image == "false")
            {
                return new ResponseMessage { Message = "Image cannot be processed,please try again later" };
            }

            _repoWrapper.InsuranceProfile.Create(updatedProfile);

            var completionProfile = new InsuranceCompletionProfile(user.Id, true, false,userProfile.InsuranceService);
            _repoWrapper.InsuranceCompletionProfile.Create(completionProfile);
            await _repoWrapper.Save();

            var newServiceString = user.ServiceUsed + ServiceNames.HealthInsured.ToString();
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
            catch (Exception)
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

            var x = PaymentReference_StatusValue.Successful.ToString();
            foreach (var item in transactionLogDTO)
            {
                if(item.Status == PaymentReference_StatusValue.Failed.ToString() || item.Status == PaymentReference_StatusValue.Successful.ToString())
                {
                    continue;
                }
                else
                {
                    item.Status = "Abandoned";
                }
            }

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
            var enrollment = await _hmoIntegrationService.AxamansardRegisterUser(enrollmentModel);
            if (!enrollment.Status)
            {
                var axaEnrollmentOnOnboarding = new EnrollmentOnOnboarding(insuranceUserProfile.Id, EnrollmentOnOnboarding_StatusValue.Failed.ToString()
                    , enrollment.Message,InsuranceProvider.Axamansard.ToString() );
                _repoWrapper.EnrollmentOnOnboarding.Create(axaEnrollmentOnOnboarding);
                await _repoWrapper.Save();
            }
            await Task.CompletedTask;
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
            var registration = await _hmoIntegrationService.HygeiaRegisterUser(registrationModel);
            if (!registration.Status)
            {
                var enrollmentOnOnboarding = new EnrollmentOnOnboarding(insuranceUserProfile.Id,EnrollmentOnOnboarding_StatusValue.Failed.ToString(), registration.Message,
                    InsuranceProvider.Hygeia.ToString());
                _repoWrapper.EnrollmentOnOnboarding.Create(enrollmentOnOnboarding);
                await _repoWrapper.Save();
                return registration;
            }
            return registration;
        }

        /// <summary>
        /// Get profile completion state
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<ResponseMessage<HealthInsuredProfileStateDTO>> GetProfileCompletion(int userId)
        {
            var profile = await _repoWrapper.InsuranceCompletionProfile.GetCompletionStateByUserId(userId);
            if (profile == null)
            {
                var corporateUser = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(userId);
                if (corporateUser != null)
                {
                    var corporateProfileState = new HealthInsuredProfileStateDTO(true, corporateUser.EmailConfirmed, corporateUser.ProfileCompleted
                        , corporateUser.TokenizationCompleted, corporateUser.InsuranceService);
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
        /// Get towns where HMO'S are  available
        /// </summary>
        /// <param name="state"></param>
        /// <param name="insuranceProvider"></param>
        /// <returns></returns>
        public ResponseMessage GetTowns(string state, string insuranceProvider)
        {            
            var townListDTO = new CityListDTO();
            if (insuranceProvider.ToLower() == InsuranceProvider.Hygeia.ToString().ToLower())
            {
                var townList = _repoWrapper.HygeiaHospitalList.GetTowns(state).Select(x => x.City).Distinct().ToList();
                townListDTO.State = state;
                townListDTO.Cities = townList;
            }
            else
            {
                var townList = _repoWrapper.AxaMansardHospitalList.GetTowns(state).Select(x => x.City).Distinct().ToList();
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
            if(insuranceProvider.ToLower() == InsuranceProvider.Axamansard.ToString().ToLower())
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

        public ResponseMessage DowloadInsuranceProfileExcelData(bool subStatus, bool activeStatus,string service)
        {
            var data = _repoWrapper.InsuranceProfile.QueryAllInsuranceProfiles();
            if(service == InsuranceProvider.Axamansard.ToString())
            {
                data = data.Where(x => x.InsuranceService == service);
            }
            if (service == InsuranceProvider.Hygeia.ToString())
            {
                data = data.Where(x => x.InsuranceService == service);
            }
            if(activeStatus == true)
            {
                data = data.Where(x => x.ActiveStatus == true);
            }
            if (activeStatus == false)
            {
                data = data.Where(x => x.ActiveStatus == false);
            }
            if (subStatus == true)
            {
                data = data.Where(x => x.SubscriptionStatus == true);
            }
            if (subStatus == false)
            {
                data = data.Where(x => x.SubscriptionStatus == false);
            }

            var dataCount = data.ToList().Count;
            var count = 3;
            if (dataCount < 1) return new ResponseMessage { Message = "Count has to be larger than zero" };
            using (var workbook = new XLWorkbook())
            {
                IXLWorksheet worksheet =
                workbook.Worksheets.Add("HealthInsurance");

                worksheet.Cell(2, 1).Value = "FullName";
                worksheet.Cell(2, 2).Value = "Email";
                worksheet.Cell(2, 3).Value = "Phonenumber";
                worksheet.Cell(2, 4).Value = "Enrollee Number";
                worksheet.Cell(2, 5).Value = "Date Of Birth";
                worksheet.Cell(2, 6).Value = "Gender";
                worksheet.Cell(2, 7).Value = "Marital Status ";
                worksheet.Cell(2, 8).Value = "Occupation";
                worksheet.Cell(2, 9).Value = "Home Address";
                worksheet.Cell(2, 10).Value = "State";
                worksheet.Cell(2, 11).Value = "Care Provider";
                worksheet.Cell(2, 12).Value = "Amount";

                foreach (var item in data.ToList())
                {
                    worksheet.Cell(count, 1).Value = item.MaidenName+" "+item.Othernames;
                    worksheet.Cell(count, 2).Value = item.Email;
                    worksheet.Cell(count, 3).Value = item.PhoneNumber;
                    worksheet.Cell(count, 4).Value = item.TransId;
                    worksheet.Cell(count, 5).Value = item.DateOfBirth.ToString();
                    worksheet.Cell(count, 6).Value = item.Gender;
                    worksheet.Cell(count, 7).Value = item.MaritalStatus;
                    worksheet.Cell(count, 8).Value = item.Occupation;
                    worksheet.Cell(count, 9).Value = item.ContactAddress;
                    worksheet.Cell(count, 10).Value = item.StateOfResidence;
                    worksheet.Cell(count, 11).Value = item.CareProviderName;
                    worksheet.Cell(count, 12).Value = item.Premium.ToString();

                    count++;
                }
                worksheet.Rows().AdjustToContents();
                worksheet.Columns().AdjustToContents();
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return new ResponseMessage { Data = content, Status = true, Message = "Excel data was fetched successfully" };
                }
            }
        }

        public async Task<ResponseMessage> UploadAxaHospitalListFromExcel(IFormFile formFile)
        {
            var presentHospitalList = _repoWrapper.AxaMansardHospitalList.GetAll().ToList();
            _repoWrapper.AxaMansardHospitalList.DeleteRange(presentHospitalList);
            await _repoWrapper.Save();
            var hospitalList = await _fileProcessor.UploadAxaHospitalListFromExcel(formFile);
            _repoWrapper.AxaMansardHospitalList.CreateRange(hospitalList);
            await _repoWrapper.Save();
            return new ResponseMessage { Status = true, Message = "Upload was successful" };
        }

        public async Task<ResponseMessage> UploadHygeiaHospitalListFromExcel(IFormFile formFile)
        {
            var presentHospitalList = _repoWrapper.HygeiaHospitalList.GetAll().ToList();
            _repoWrapper.HygeiaHospitalList.DeleteRange(presentHospitalList);
            await _repoWrapper.Save();
            var hospitalList = await _fileProcessor.UploadHygeiaHospitalListFromExcel(formFile);
            _repoWrapper.HygeiaHospitalList.CreateRange(hospitalList);
            await _repoWrapper.Save();
            return new ResponseMessage { Status = true, Message = "Upload was successful" };
        }

        public void SendEmailReminder(string email, string userName, PerformContext context)
        {
            _emailSender.SendHealthInsuredPaymentReminder(email, "Payment Reminder", userName);
        }

        /// <summary>
        /// Function to send successful subscription email.Send this only to user when subscription status is null
        /// </summary>
        /// <param name="email"></param>
        /// <param name="userName"></param>
        /// <param name="enroleeNumber"></param>
        /// <param name="healthCareProvider"></param>
        public void SendSuccesfulSubscriptionMail(string email, string userName, string enroleeNumber, string healthCareProvider,string plan)
        {
            _emailSender.HealthInsuredSubscriptionMail(email, "Active Free Trial", userName, enroleeNumber, healthCareProvider,plan);
        }

        public void SendEmailOnFailedDebit(string email, string userName, string premium)
        {
            _emailSender.HealthInsuredFailedDebit(email, "Failed Transaction", userName, premium);
        }

        public void SendCompanyEmailOnFailedDebit(string email, string userName, string premium, string stopDate)
        {
            _emailSender.HealthInsuredFailedCompanyDebit(email, "Failed Transaction", userName, premium, stopDate);
        }

        public void SendCompanyDeactivationMail(string email, string userName, string premium)
        {
            _emailSender.HealthInsuredCompanyDeactivation(email, "Deactivate Beneficiaries", userName, premium);
        }        
        
        public string GetUniqueCode()
        {
            return _uniqueIdentifier.GetUniqueCode(10);
        }
    }

    
}
