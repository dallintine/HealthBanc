using Application.DTO;
using Application.DTO.HealthInsured_AxaMansard;
using Application.HealthInsured_AxaMansard_Service.Insurance;
using Application.Helpers;
using Application.Interfaces;
using Application.ViewModels.HealthInsured;
using AutoMapper;
using DataAccess;
using Domain.Models;
using Domain.Models.Axa.Hygeia_Insurance;
using Domain.Models.Axa_Hygeia_Insurance;
using Domain.Models.ReportAndLogs;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.HealthInsured.Insurance
{
    public class CorporateInsuranceService
    {
        private readonly IMapper _mapper;
        private readonly ILogger<CorporateInsuranceService> _logger;
        private readonly IFileProcessor _fileProcessor;
        private readonly IRepositoryWrapper _repoWrapper;
        private readonly IUniqueIdentifier _uniqueIdentifier;
        private readonly IEmailSender _emailSender;
        private readonly InsuranceService _insuranceService;

        public UserManager<ApplicationUser> UserManager { get; }
        private SubscriptionDuration SubscriptionAccessor { get; }

        public CorporateInsuranceService(IMapper mapper, ILogger<CorporateInsuranceService> logger, IFileProcessor fileProcessor, IRepositoryWrapper repoWrapper, IUniqueIdentifier uniqueIdentifier,
            IOptions<SubscriptionDuration> subscriptionAccessor, UserManager<ApplicationUser> userManager, IEmailSender emailSender,InsuranceService insuranceService)
        {
            _mapper = mapper;
            _logger = logger;
            _fileProcessor = fileProcessor;
            _repoWrapper = repoWrapper;
            _uniqueIdentifier = uniqueIdentifier;
            UserManager = userManager;
            _emailSender = emailSender;
            _insuranceService = insuranceService;
            SubscriptionAccessor = subscriptionAccessor.Value;
        }

        public async Task<ResponseMessage> CreateCorporateUser(CorporateRegistrationViewModel corporateRegViewModel, int userId)
        {
            var checkIfUserIsRegisteredAsIndividualUser = await _repoWrapper.InsuranceProfile.GetByUserIdAsync(userId);
            if (!(checkIfUserIsRegisteredAsIndividualUser is null)) return new ResponseMessage { Status = false, Message = "User is registered as an individual user" };

            var company = await _repoWrapper.CompanyProfile.GetCompanyProfileByEmail(corporateRegViewModel.Email);

            if (company != null)
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
                InsuranceService = corporateRegViewModel.InsuranceProvider.ToLower() == "hygeia" ? InsuranceProvider.Hygeia.ToString() : InsuranceProvider.Axamansard.ToString()
            };

            // Schedule otp removal after 2 minutes
            var otpJobId = BackgroundJob.Schedule(() => RemoveOTP(userId), DateTime.Now.AddMinutes(2));

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
        public async Task<ResponseMessage> UploadUserProfileFromExcelFile(IFormFile file, int companyUserId)
        {
            var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(companyUserId);

            if (companyProfile.EmailConfirmed)
            {
                // Process excel list
                var excelModel = await _fileProcessor.ProcessUserProfileFromExcelFile(file);

                // If Excel file is not the one supplied from the application
                if (excelModel is null)
                {
                    return new ResponseMessage { Message = "Excel file is not supported. Note: Kindly download excel file from your dashboard." };
                }

                if (excelModel.Count == 0)
                {
                    return new ResponseMessage { Message = "No data was read. Kindly add data to excel file" };
                }

                // Get company beneficaries from excel list
                var companyBeneficiaries = _mapper.Map<List<FileModel>, List<BeneficiaryReviewUser>>(excelModel);

                var checkForDisitnctBeneficiaryReviewEmail = companyBeneficiaries.Distinct(new BeneficiaryReviewUserEmailComparer());

                if (companyBeneficiaries.Count != checkForDisitnctBeneficiaryReviewEmail.ToList().Count)
                {
                    return new ResponseMessage { Message = "Similar email was found in different rows, please go through the excel data and make sure emails are unique in all rows!" };
                }

                var newCompanyBeneficiaries = new List<BeneficiaryReviewUser>();
                // If card is not tokenized we add users in the excel sheet to list of beneficiary Review users
                if (!companyProfile.TokenizationCompleted)
                {
                    foreach (var item in companyBeneficiaries)
                    {
                        //Check beneficiary review list for existing email
                        var beneficiaryReview = await _repoWrapper.BeneficiaryReview.GetByEmail(item.Email);
                        //Check insurance profile list for existing email
                        var beneficiary = await _repoWrapper.InsuranceProfile.GetByEmail(item.Email);

                        if (beneficiaryReview is null && beneficiary is null)
                        {
                            item.CompanyProfileId = companyProfile.Id;
                            item.Amount = decimal.Parse("1000");
                            item.PhoneNumber = item.PhoneNumber.StartsWith("0") ? item.PhoneNumber : "0" + item.PhoneNumber;
                            newCompanyBeneficiaries.Add(item);
                        }
                    }

                    if (newCompanyBeneficiaries.Count > 0)
                    {
                        await _repoWrapper.BeneficiaryReview.InsertEntities(newCompanyBeneficiaries);
                    }

                    var beneficaryReviewUser = await _repoWrapper.BeneficiaryReview.FilterBeneficiariesReview(new PaginationQuery(), companyProfile.Id);
                    var beneficiariesReviewDTO = _mapper.Map<IEnumerable<BeneficiaryReviewUser>, IEnumerable<BeneficiaryReviewDTO>>(beneficaryReviewUser.Data);

                    var companyRevieews = await _repoWrapper.CompanyProfile.GetCompanyBeneficiaryReviewUsersByCompanyId(companyProfile.Id);
                    var totalAmount = companyRevieews.BeneficiaryReviewUsers.Where(x => x.IsRemove == false).Select(x => x.Amount).Sum();

                    var paginatedResponse = new PagedResponse<BeneficiaryReviewDTO>
                    {
                        Amount = totalAmount,
                        Data = beneficiariesReviewDTO,
                        PageNumber = 1,
                        PageSize = 50,
                        RecordCount = beneficaryReviewUser.RecordCount,
                        PageCount = beneficaryReviewUser.PageCount
                    };

                    if (newCompanyBeneficiaries.Count != companyBeneficiaries.Count)
                    {
                        return new ResponseMessage
                        {
                            Data = paginatedResponse,
                            Message = "Some beneficiaries could not be added to the beneficiay review list as their emails are duplicate of existing users",
                            Status = true
                        };
                    }

                    return new ResponseMessage
                    {
                        Data = paginatedResponse,
                        Message = "Beneficiairies was added sucessfully",
                        Status = true
                    };
                }
                // Else we process users in the excel sheet to be added to the Beneficaries List either as pending or active beneficiairy
                return await CreateInsuranceProfileForCompanyBeneficiaries(companyProfile.Id, companyBeneficiaries);
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
            // Count for existing duplicate emails
            int checkIfProfileEmailExistCount = 0;
            var companySubscribedStatus = InsuranceProfile_CompanySubStatusValue.Pending.ToString();
            var companyprofile = await _repoWrapper.CompanyProfile.GetCompanyBeneficiaryReviewUsersByCompanyId(companyId);

            // if beneficaryReview is null, means user just made payment and subscription status is set to active
            if (beneficiaryReviews is null)
            {
                beneficiaryReviews = companyprofile.BeneficiaryReviewUsers.Where(x => x.IsRemove == false).ToList();
                companySubscribedStatus = InsuranceProfile_CompanySubStatusValue.Active.ToString();
            }
            var insuranceUserProfiles = new List<InsuranceUserProfile>();
            foreach (var item in beneficiaryReviews)
            {
                // if email exist incerease checkIfProfileEmailExistCount count               
                var checkUserEmail = await UserManager.FindByEmailAsync(item.Email);
                if (!(checkUserEmail is null))
                {
                    checkIfProfileEmailExistCount++;
                    continue;
                }
                if (checkUserEmail == null)
                {
                    var user = _mapper.Map<ApplicationUser>(item);
                    var result = await UserManager.CreateAsync(user);
                    if (result.Succeeded)
                    {
                        var newServiceString = user.ServiceUsed + ServiceNames.HealthInsured.ToString();
                        user.ServiceUsed = newServiceString;
                        await UserManager.UpdateAsync(user);
                        await UserManager.AddToRoleAsync(user, "SuperAdmin");

                        var insuranceUserProfile = _mapper.Map<InsuranceUserProfile>(item);
                        insuranceUserProfile.UserId = user.Id;
                        insuranceUserProfile.CompanyProfileId = companyId;
                        insuranceUserProfile.InsuranceService = companyprofile.InsuranceService;
                        insuranceUserProfile.Premium = Decimal.Parse("1000");
                        insuranceUserProfile.CompanySubscribedStatus = companySubscribedStatus;
                        insuranceUserProfile.CompanyName = companyprofile.CompanyName;
                        insuranceUserProfile.PhoneNumber = insuranceUserProfile.PhoneNumber.StartsWith("0") ? insuranceUserProfile.PhoneNumber : "0" + insuranceUserProfile.PhoneNumber;
                        if (companySubscribedStatus == InsuranceProfile_CompanySubStatusValue.Active.ToString())
                        {
                            insuranceUserProfile.ActiveStatus = true;
                            insuranceUserProfile.SubscriptionStatus = true;
                            insuranceUserProfile.StartActiveStatusDate = DateTime.Now;
                            insuranceUserProfile.EndActiveStatusDate = DateTime.Now.AddDays(SubscriptionAccessor.FreeTrialDayDuration);
                        }
                        insuranceUserProfiles.Add(insuranceUserProfile);
                    }
                }
            }
            _repoWrapper.InsuranceProfile.CreateRange(insuranceUserProfiles);

            var beneficiaries = companyprofile.BeneficiaryReviewUsers.ToList();
            // Delete all beneficiary reveiews after moving users to the insurance profile list
            if (beneficiaries.Count > 0)
            {
                _repoWrapper.BeneficiaryReview.DeleteRange(beneficiaries);
            }
            // if at least one beneficiary was moved to insurace profile list
            if (checkIfProfileEmailExistCount != beneficiaryReviews.Count)
            {
                var activityLog = new ActivityLog(null, companyprofile.Id, "New Beneficiairies Was Added", companyprofile.InsuranceService);
                _repoWrapper.ActivityLog.Create(activityLog);
            }

            await _repoWrapper.Save();
            // if duplicate emails do not exist
            if (checkIfProfileEmailExistCount is 0)
            {
                return new ResponseMessage { Message = "Beneficiaries was added successfully", Status = true };
            }
            // if duplicate emails exist
            return new ResponseMessage { Message = "Beneficiaries was added successfully,however " + checkIfProfileEmailExistCount + " beneficiary could not be added as their email already exist!", Status = true };
        }

        public async Task OnboardCompanyUsersToHMO(int companyUserId, string status, DateTime endActiveStatusDate, string insuranceProvider)
        {
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
                if(insuranceProvider.ToLower() == InsuranceProvider.Hygeia.ToString().ToLower())
                {
                    insuranceUserProfiles = insuranceProfiles.Where(x => x.CompanySubscribedStatus == status).ToList();
                }
                else
                {
                    insuranceUserProfiles = insuranceProfiles.Where(x => x.CompanySubscribedStatus == InsuranceProfile_CompanySubStatusValue.Pending.ToString() ||
                    x.CompanySubscribedStatus == InsuranceProfile_CompanySubStatusValue.Active.ToString()).ToList();
                }
            }

            foreach (var item in insuranceUserProfiles)
            {
                if (insuranceProvider.ToLower() == InsuranceProvider.Hygeia.ToString().ToLower())
                {
                    var result = await _insuranceService.EnrollUserToHygeiaOnOnboarding(item);
                    if (result.Status)
                    {
                        item.TransId = result.Message;
                    }
                }
                else
                {
                    await _insuranceService.EnrollUserToAxamansardOnOnboarding(item);
                    item.TransId = _uniqueIdentifier.GetUniqueCode(10);
                }
                item.ActiveStatus = true;
                item.SubscriptionStatus = true;
                item.StartActiveStatusDate = DateTime.Now;
                item.EndActiveStatusDate = endActiveStatusDate;
                item.CompanySubscribedStatus = InsuranceProfile_CompanySubStatusValue.Active.ToString();
                _repoWrapper.InsuranceProfile.Update(item);
            }
            await _repoWrapper.Save();
            await Task.CompletedTask;
        }

        public async Task<ResponseMessage> ConfirmOtp(string otp, int userId)
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
                        var user = await UserManager.FindByIdAsync(userId.ToString());
                        user.ServiceUsed += ServiceNames.HealthInsured.ToString();
                        _repoWrapper.ApplicationUser.Update(user);
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
                return new ResponseMessage { Message = "Company profile does not exist.", Status = false };
            }
            else if (company.EmailConfirmed)
            {
                return new ResponseMessage { Message = "Email was confirmed previously", Status = false };
            }
            var otp = _uniqueIdentifier.GetUniqueCode(6);
            company.OTPCode = otp;
            if (company.OTPJobId != null)
            {
                BackgroundJob.Delete(company.OTPJobId);
            }
            // Schedule otp removal after 5 minutes
            var otpJobId = BackgroundJob.Schedule(() => RemoveOTP(userId), DateTime.Now.AddMinutes(2));

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

        public async Task<ResponseMessage> UpdateCorporateUser(UpdateCorporateUserViewModel updateCorporateUserViewModel, int Id)
        {
            var company = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(Id);
            if (company is null)
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
            return new ResponseMessage { Message = "Company profile was updated successfully", Status = true, Data = corporateprofileDTO };
        }

        public async Task<ResponseMessage> GetBeneficiariesReview(PaginationQuery paginationQuery, int companyUserId)
        {
            var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(companyUserId);

            var companyRevieews = await _repoWrapper.CompanyProfile.GetCompanyBeneficiaryReviewUsersByCompanyId(companyProfile.Id);
            var totalAmount = companyRevieews.BeneficiaryReviewUsers.Where(x => x.IsRemove == false).Select(x => x.Amount).Sum();

            var companyBeneficiaries = await _repoWrapper.BeneficiaryReview.FilterBeneficiariesReview(paginationQuery, companyProfile.Id);
            var beneficiariesReviewDTO = _mapper.Map<IEnumerable<BeneficiaryReviewUser>, IEnumerable<BeneficiaryReviewDTO>>(companyBeneficiaries.Data);

            var paginatedResponse = new PagedResponse<BeneficiaryReviewDTO>
            {
                Data = beneficiariesReviewDTO,
                Amount = totalAmount,
                PageNumber = paginationQuery.PageNumber >= 1 ? paginationQuery.PageNumber : (int?)null,
                PageSize = paginationQuery.PageSize >= 1 ? paginationQuery.PageSize : (int?)null,
                RecordCount = companyBeneficiaries.RecordCount,
                PageCount = companyBeneficiaries.PageCount
            };
            return new ResponseMessage { Data = paginatedResponse, Status = true, Message = "Beneficiaries was fetched successfully" };
        }

        public async Task<ResponseMessage> GetCompanyBeneficiaries(PaginationQuery paginationQuery, int companyUserId)
        {
            var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(companyUserId);

            var beneficiaries = await _repoWrapper.InsuranceProfile.GetAllInsuranceProfileUnderCompany(paginationQuery, companyProfile.Id);
            var beneficiariesDTO = _mapper.Map<IEnumerable<InsuranceUserProfile>, List<InsuranceBeneficiaryDTO>>(beneficiaries.Data);

            var paginatedResponse = new PagedResponse<InsuranceBeneficiaryDTO>
            {
                Data = beneficiariesDTO,
                Amount = beneficiaries.RecordCount * 1000,
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
            foreach (var item in beneficiaryListViewModel.Emails)
            {
                var insuranceUserProfile = await _repoWrapper.InsuranceProfile.GetByEmail(item);
                if (insuranceUserProfile != null)
                {
                    if (insuranceUserProfile.CompanySubscribedStatus == InsuranceProfile_CompanySubStatusValue.Inactive.ToString())
                    {
                        insuranceUserProfile.CompanySubscribedStatus = InsuranceProfile_CompanySubStatusValue.Pending.ToString();
                        _repoWrapper.InsuranceProfile.Update(insuranceUserProfile);
                        _repoWrapper.CompanyProfile.Update(companyprofile);
                    }
                }
            }
            await _repoWrapper.Save();
            return new ResponseMessage { Status = true, Message = "Beneficiary was moved to pending list succesfully.Users will become active in the next cycle" };
        }

        public async Task<ResponseMessage> MoveCompanyBeneficiaryFromPendingToInactiveState(BeneficiaryListViewModel beneficiaryListViewModel, int userId)
        {
            var companyprofile = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(userId);
            foreach (var item in beneficiaryListViewModel.Emails)
            {
                var insuranceUserProfile = await _repoWrapper.InsuranceProfile.GetByEmail(item);
                if (insuranceUserProfile != null)
                {
                    if (insuranceUserProfile.CompanySubscribedStatus == InsuranceProfile_CompanySubStatusValue.Pending.ToString())
                    {
                        insuranceUserProfile.CompanySubscribedStatus = InsuranceProfile_CompanySubStatusValue.Inactive.ToString();
                        insuranceUserProfile.ActiveStatus = false;
                        _repoWrapper.InsuranceProfile.Update(insuranceUserProfile);
                        _repoWrapper.CompanyProfile.Update(companyprofile);
                    }
                }
            }
            await _repoWrapper.Save();
            return new ResponseMessage { Status = true, Message = "Beneficiary was moved to the inactive list succesfully" };
        }

        public async Task<ResponseMessage> CorporateSubscribersAnalytics(int companyUserId)
        {
            var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyInsuranceUserProfilesByUserId(companyUserId);

            var activeBeneficiaryCount = companyProfile.InsuranceUserProfiles.Where(x => x.ActiveStatus == true).Count();
            var pendingbeneficiarycount = companyProfile.InsuranceUserProfiles.Where(x => x.CompanySubscribedStatus == InsuranceProfile_CompanySubStatusValue.Pending.ToString()).Count();
            var inactiveBeneficairyCount = companyProfile.InsuranceUserProfiles.Where(x => x.ActiveStatus == false && x.CompanySubscribedStatus == InsuranceProfile_CompanySubStatusValue.Inactive.ToString()).Count();

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

        public async Task<ResponseMessage> RemoveCompanyBeneficiary(string email, int companyUserId)
        {
            var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyProfileByUserId(companyUserId);
            var beneficiary = await _repoWrapper.BeneficiaryReview.GetByEmail(email);
            if (beneficiary.CompanyProfileId == companyProfile.Id)
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
    }
    internal class BeneficiaryReviewUserEmailComparer : IEqualityComparer<BeneficiaryReviewUser>
    {
        public bool Equals(BeneficiaryReviewUser x, BeneficiaryReviewUser y)
        {
            if (string.Equals(x.Email, y.Email, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }

        public int GetHashCode(BeneficiaryReviewUser obj)
        {
            return obj.Email.GetHashCode();
        }
    }
}
