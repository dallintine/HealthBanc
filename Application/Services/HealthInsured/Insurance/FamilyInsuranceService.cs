using Application.DTO;
using Application.DTO.HealthInsured_AxaMansard;
using Application.Interfaces;
using Application.ViewModels.HealthInsured;
using AutoMapper;
using DataAccess;
using Domain.Models;
using Domain.Models.Axa_Hygeia_Insurance;
using Hangfire.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.HealthInsured.Insurance
{
    public class FamilyInsuranceService
    {
        private readonly IRepositoryWrapper _repoWrapper;
        private readonly IMapper _mapper;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<FamilyInsuranceService> _logger;

        public FamilyInsuranceService(IRepositoryWrapper repoWrapper,IMapper mapper, UserManager<ApplicationUser> userManager,IEmailSender emailSender,
            ILogger<FamilyInsuranceService> logger)
        {
            _repoWrapper = repoWrapper;
            _mapper = mapper;
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task<ResponseMessage> CreateFamilyProfile(int id, string insuranceService)
        {
            _logger.LogInformation($"Creating Family Profile : [UserId : {id} | InsuranceService : {insuranceService}]\n");
            var user = await _repoWrapper.ApplicationUser.FindByIdAsync(id);
            var family = _mapper.Map<FamilyProfile>(user);
            family.ProfileCompleted = true;
            family.InsuranceService = InsuranceProvider.Axamansard.ToString();
            _repoWrapper.FamilyProfile.Create(family);
            _repoWrapper.ApplicationUser.Update(user);
            await _repoWrapper.Save();
            _logger.LogInformation($"Member Profile was created successfully\n");
            return new ResponseMessage { Status = true, Message = "Profile was created successfully" };
        }

        public async Task<ResponseMessage> CreateInsuranceProfileForFamilyMemeber(FamilyMemberViewModel familyMemberViewModel, int userId)
        {
            _logger.LogInformation($"Create insurance Profile For Family Member [FamilyMember :{JsonConvert.SerializeObject(familyMemberViewModel)} | " +
                $"Userid : {userId}]\n");
            var familyCreator = await _repoWrapper.FamilyProfile.GetByUserId(userId);
            var insuranceProfile = _mapper.Map<InsuranceUserProfile>(familyMemberViewModel);
            insuranceProfile.PhoneNumber = familyCreator.PhoneNumber;
            insuranceProfile.FamilyProfileId = familyCreator.Id;
            insuranceProfile.FamilyEmail = familyCreator.Email;
            insuranceProfile.InsuranceService = InsuranceProvider.Axamansard.ToString();
            insuranceProfile.ActiveStatus = null;
            insuranceProfile.SubscriptionStatus = null;
            _repoWrapper.InsuranceProfile.Create(insuranceProfile);
            await _repoWrapper.Save();
            _logger.LogInformation($"Family Member was added Successfully\n");
            return new ResponseMessage { Status = true, Message = "Member was added successfully" };
        }

        public async Task<ResponseMessage> GetFamilyMembers(PaginationQuery paginationQuery, int userId)
        {
            var paginatedResponse = await _repoWrapper.FamilyProfile.PaginatedFamilyMembers(paginationQuery, userId);
            var paginatedResponseDTO = _mapper.Map<IEnumerable<InsuranceUserProfile>, IEnumerable<FamilyMembersDTO>>(paginatedResponse.Data);
            var pagedResponse = new PagedResponse<FamilyMembersDTO>()
            {
                Data = paginatedResponseDTO,
                RecordCount = paginatedResponse.RecordCount,
                PageCount = paginatedResponse.PageCount,
                PageNumber = paginatedResponse.PageNumber,
                PageSize = paginatedResponse.PageSize
            };
            return new ResponseMessage { Data = pagedResponse, Message = "Members was fetched successfully", Status = true };
        }

        public async Task<ResponseMessage> UpdateFamilyMember(int userId,int famiyMemeberId, FamilyMemberViewModel familyMemberViewModel)
        {
            var familyProfile = await _repoWrapper.FamilyProfile.GetExtendedFamilyDetails(userId);
            if (familyProfile != null)
            {
                var insuranceProfile = familyProfile.InsuranceUserProfiles.Where(x => x.Id == famiyMemeberId).FirstOrDefault();
                if (insuranceProfile != null)
                {
                    insuranceProfile = _mapper.Map(familyMemberViewModel, insuranceProfile);
                    _repoWrapper.InsuranceProfile.Update(insuranceProfile);
                    await _repoWrapper.Save();
                    return new ResponseMessage { Message = "Family Member profile was updated successfully", Status = true };
                }
                return new ResponseMessage { Message = "Family Member does not exist under your current profile" };
            }
            return new ResponseMessage { Message = "Profile does not exist" };            
        }

        public async Task<ResponseMessage> RemoveFamilyMember(int userId, int famiyMemeberId)
        {
            _logger.LogInformation($"Removing Family Member [userId : {userId} | familuMemberId : {famiyMemeberId}]\n");

            var familyProfile = await _repoWrapper.FamilyProfile.GetExtendedFamilyDetails(userId);
            if(familyProfile != null)
            {
                var insuranceProfile = familyProfile.InsuranceUserProfiles.Where(x => x.Id == famiyMemeberId).FirstOrDefault();
                if (insuranceProfile != null)
                {
                    if(insuranceProfile.ActiveStatus  != true)
                    {
                        _repoWrapper.InsuranceProfile.Delete(insuranceProfile);
                        await _repoWrapper.Save();
                        _logger.LogInformation($"Remove Family member was successfully\n");
                        return new ResponseMessage { Message = "Member profile was removed successfully", Status = true };
                    }
                    _logger.LogInformation($"Remove terminated : An active family member cannot be removed\n");
                    return new ResponseMessage { Message = "An active memeber cannot be removed,Deactivate user and wait till insurance cycle ends" };
                }
                _logger.LogInformation($"Family memeber insurance profile do not exist\n");
                return new ResponseMessage { Message = "Member does not exist under your current profile" };
            }
            _logger.LogInformation($"Family profile do not exist\n");
            return new ResponseMessage { Message = "Profile does not exist" };
           
        }

        public async Task SendPaymentReminder(string email, string familyHead,string familyMember, PerformContext context)
        {
            await _emailSender.SendHealthInsuredFamilyPaymentReminder(email, "HealthInsured Payment Reminder", familyHead, familyMember);
        }

        public async Task FamilySubscription(string email,string subject, string username)
        {
            await _emailSender.FamilySubscription(email, subject, username);
        }
    }
}
