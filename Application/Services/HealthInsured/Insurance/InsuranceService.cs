using Application.API_RequestModel.HealthInsured;
using Application.API_ResponseModel.HealthInsured;
using Application.AuditAndReport.AuditLog;
using Application.DTO;
using Application.DTO.HealthInsured_AxaMansard;
using Application.Helpers;
using Application.Interfaces;
using Application.Services.HealthInsured;
using Application.Services.HealthInsured.Insurance;
using Application.Services.Identity;
using Application.ViewModels;
using Application.ViewModels.HealthInsured;
using AutoMapper;
using ClosedXML.Excel;
using DataAccess;
using Domain.Enums;
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
            IFileProcessor fileProcessor, IRepositoryWrapper repoWrapper, UserManager<ApplicationUser> userManager, HMOIntegrationService hmoIntegrationService,
            IImageService imageService)
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

            var activityLog = new ActivityLog(checkIfUserHasBeenProfiled.Id, null,null, "Updated HealthInsured Profile", ServiceNames.HealthInsured.ToString());
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

        public async Task<ResponseMessage> GetReferedInsuranceProfiles(PaginationQuery paginationQuery,int userId)
        {
            var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByUserIdAsync(userId);
            var insuranceProfiles = await _repoWrapper.InsuranceProfile.GetPaginatedReferredInsuranceProfiles(paginationQuery,insuranceProfile.Id);

            var insuranceProfilesDTO = _mapper.Map<IEnumerable<InsuranceUserProfile>, IEnumerable<IndividualProfileDTO>>(insuranceProfiles.Data);
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

        public async Task<ResponseMessage> RemovePaidReferee(int userId, string email)
        {
            var payeeInsuranceProfile = await _repoWrapper.InsuranceProfile.GetByUserIdAsync(userId);
            var refereedInsuranceProfile = await _repoWrapper.InsuranceProfile.GetByEmail(email);
            if(refereedInsuranceProfile != null)
            {
                if(refereedInsuranceProfile.InsurancePayeeId == payeeInsuranceProfile.Id)
                {
                    if(refereedInsuranceProfile.SubscriptionStatus is true)
                    {
                        return new ResponseMessage { Message = "Referee has to be deactivated before removing!." };
                    }
                    if (refereedInsuranceProfile.ActiveStatus is true)
                    {
                        return new ResponseMessage { Message = "Referee has to be Inactive before removing!." };
                    }
                    //Mean refereedInsuranceProfile insurance profile is completed
                    if (refereedInsuranceProfile.ContactAddress != null){
                        refereedInsuranceProfile.InsurancePayeeId = null;
                        _repoWrapper.InsuranceProfile.Update(refereedInsuranceProfile);
                    }
                    else
                    {
                        _repoWrapper.InsuranceProfile.Delete(refereedInsuranceProfile);
                    }
                    await _repoWrapper.Save();
                    return new ResponseMessage { Message = "Referee was removed successfully", Status=true };
                }
                return new ResponseMessage { Message = "Insurance profile of referee does not exist"};
            }
            return new ResponseMessage { Message = "Insurance profile of referee does not exist" };
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

        /// <summary>
        /// Get profile completion state
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<ResponseMessage<HealthInsuredProfileStateDTO>> GetProfileCompletion(int? userId,string email)
        {
            var notFoundProfileState = new HealthInsuredProfileStateDTO(null, null, false, false, null);
            var processIndvidualProfileState =  await ProcessIndividualProfileCompletion(userId, email);
            if(processIndvidualProfileState.Status)
            {
                return processIndvidualProfileState;
            }
            if(userId != null)
            {
                var familyUser = await _repoWrapper.FamilyProfile.GetByUserId(userId.Value);
                if (familyUser != null)
                {
                    var familyProfileState = new HealthInsuredProfileStateDTO(HealthInsuredPlan.Family, familyUser.EmailConfirmed, familyUser.ProfileCompleted
                        , familyUser.TokenizationCompleted, familyUser.InsuranceService);
                    return new ResponseMessage<HealthInsuredProfileStateDTO>
                    {
                        Data = familyProfileState,
                        Status = true,
                        Message = "Profile completion state was fetched successfully"
                    };
                }
                var corporateUser = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(userId.Value);
                if (corporateUser != null)
                {
                    var corporateProfileState = new HealthInsuredProfileStateDTO(HealthInsuredPlan.Corporate, corporateUser.EmailConfirmed, corporateUser.ProfileCompleted
                        , corporateUser.TokenizationCompleted, corporateUser.InsuranceService);
                    return new ResponseMessage<HealthInsuredProfileStateDTO>
                    {
                        Data = corporateProfileState,
                        Status = true,
                        Message = "Profile completion state was fetched successfully"
                    };
                }               
                return new ResponseMessage<HealthInsuredProfileStateDTO>
                {
                    Data = notFoundProfileState,
                    Status = false,
                    Message = "Profile completion state was fetched successfully"
                };
            }
            return new ResponseMessage<HealthInsuredProfileStateDTO>
            {
                Data = notFoundProfileState,
                Status = false,
                Message = "Profile completion state was fetched successfully"
            };

        }

        private async Task<ResponseMessage<HealthInsuredProfileStateDTO>> ProcessIndividualProfileCompletion(int? userId, string email)
        {
            if(userId != null)
            {
                var profile = await _repoWrapper.InsuranceCompletionProfile.GetCompletionStateByUserId(userId.Value);
                if (profile != null)
                {
                    var individualProfileState = new HealthInsuredProfileStateDTO(HealthInsuredPlan.Individual, null, profile.ProfileCompleted, profile.TokenizationCompleted, profile.ServiceUsed);
                    return new ResponseMessage<HealthInsuredProfileStateDTO>
                    {
                        Data = individualProfileState,
                        Status = true,
                        Message = "Profile completion state was fetched successfully"
                    };
                }
                //// if user is a referee dat was paid with full details
                //var usedProfile = await _repoWrapper.InsuranceProfile.GetByUserIdAsync(userId.Value);
                //if (usedProfile != null)
                //{
                //    if (usedProfile.ContactAddress != null)
                //    {
                //        var individualProfileState = new HealthInsuredProfileStateDTO(HealthInsuredPlan.Individual, true, true, true, usedProfile.InsuranceService);
                //        return new ResponseMessage<HealthInsuredProfileStateDTO>
                //        {
                //            Data = individualProfileState,
                //            Status = true,
                //            Message = "Profile completion state was fetched successfully"
                //        };
                //    }                   
                //}
            }
            if(email != null)
            {
                var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByEmail(email);
                if(insuranceProfile != null)
                {
                    if (insuranceProfile.InsurancePayeeId != null)
                    {
                        if (insuranceProfile.ContactAddress != null)
                        {
                            return new ResponseMessage<HealthInsuredProfileStateDTO>
                            {
                                Data = new HealthInsuredProfileStateDTO(HealthInsuredPlan.Individual, null, true, true, insuranceProfile.InsuranceService,true),
                                Status = true,
                                Message = "Profile completion state was fetched successfully"
                            };
                        }
                        return new ResponseMessage<HealthInsuredProfileStateDTO>
                        {
                            Data = new HealthInsuredProfileStateDTO(HealthInsuredPlan.Individual, null, false, true, insuranceProfile.InsuranceService,true),
                            Status = true,
                            Message = "Profile completion state was fetched successfully"
                        };
                    }
                }               
            }
            return new ResponseMessage<HealthInsuredProfileStateDTO>
            {
                Status = false
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
        public async Task<ResponseMessage> PayforNewIndividualWithEmail(int userId,string email, string insuranceService)
        {            
            var userToPayFor = await _repoWrapper.ApplicationUser.FindByEmailAsync(email);
            var insuranceProfileOfUserPaying = await _repoWrapper.InsuranceProfile.GetByUserIdAsync(userId);
            if(!(insuranceProfileOfUserPaying.Cards.Any(x => x.Status == (int)DebitCard_StatusValue.primary)))
            {
                return new ResponseMessage { Message = "Kindly set an active card before transaction can be initiated" };
            }
            if(userToPayFor is null || userToPayFor.ServiceUsed is null || !userToPayFor.ServiceUsed.Equals(ServiceNames.HealthInsured.ToString()))
            {
                var profile = await _repoWrapper.InsuranceProfile.GetByEmail(email);
                if (profile is null)
                {
                    var insuranceProfile = new InsuranceUserProfile();
                    insuranceProfile.Email = email;
                    insuranceProfile.InsuranceService = insuranceService;
                    insuranceProfile.PlanCode = "1";
                    insuranceProfile.Premium = Decimal.Parse("1000");
                    insuranceProfile.InsurancePayeeId = insuranceProfileOfUserPaying.Id;                  
                    _repoWrapper.InsuranceProfile.Create(insuranceProfile);
                    await _repoWrapper.Save();
                    _emailSender.RefreeInvitation(insuranceProfile.Email, "HealthInsured Gift", $"{insuranceProfileOfUserPaying.Othernames} {insuranceProfileOfUserPaying.Surname}");
                    return new ResponseMessage
                    {
                        Message = "Invite was sent successfully. User would be activated immediately after sign up/profile creation " +
                        "with a one month free cycle.",
                        Status= true
                    };
                }
                return new ResponseMessage { Message = "HealthInsured profile with email exist already", Status = false };
            }
            return new ResponseMessage { Message = "HealthInsured profile with this email exist already", Status = false };
        }

        /// <summary>
        /// Get hygeia access token
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task HygeiaGetAuthToken()
        {
            await _hmoIntegrationService.HygeiaGetAuthToken();            
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

        public void SendEmailOnFailedDebit(string email, string userName, string premium,string info)
        {
            _emailSender.HealthInsuredDeactivationNotification(email, "Failed Transaction", userName, premium,info);
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
