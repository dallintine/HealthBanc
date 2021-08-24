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

        public FamilyInsuranceService(IRepositoryWrapper repoWrapper,IMapper mapper, UserManager<ApplicationUser> userManager,IEmailSender emailSender)
        {
            _repoWrapper = repoWrapper;
            _mapper = mapper;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        public async Task<ResponseMessage> CreateFamilyProfile(int id, string insuranceService)
        {
            var user = await _repoWrapper.ApplicationUser.FindByIdAsync(id);
            var family = _mapper.Map<FamilyProfile>(user);
            family.ProfileCompleted = true;
            family.InsuranceService = InsuranceProvider.Axamansard.ToString();
            _repoWrapper.FamilyProfile.Create(family);
            _repoWrapper.ApplicationUser.Update(user);
            await _repoWrapper.Save();
            return new ResponseMessage { Status = true, Message = "Profile was created successfully" };
        }

        public async Task<ResponseMessage> CreateInsuranceProfileForFamilyMemeber(FamilyMemberViewModel familyMemberViewModel, int userId)
        {
            var familyCreator = await _repoWrapper.FamilyProfile.GetByUserId(userId);
            var insuranceProfile = _mapper.Map<InsuranceUserProfile>(familyMemberViewModel);
            insuranceProfile.FamilyProfileId = familyCreator.Id;
            insuranceProfile.InsuranceService = InsuranceProvider.Axamansard.ToString();
            _repoWrapper.InsuranceProfile.Create(insuranceProfile);
            await _repoWrapper.Save();
            return new ResponseMessage { Status = true, Message = "Family member was added successfully" };
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
            return new ResponseMessage { Data = pagedResponse, Message = "Family members was fetched successfully", Status = true };
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
            return new ResponseMessage { Message = "Family Profile does not exist" };            
        }

        public async Task<ResponseMessage> RemoveFamilyMember(int userId, int famiyMemeberId)
        {
            var familyProfile = await _repoWrapper.FamilyProfile.GetExtendedFamilyDetails(userId);
            if(familyProfile != null)
            {
                var insuranceProfile = familyProfile.InsuranceUserProfiles.Where(x => x.Id == famiyMemeberId).FirstOrDefault();
                if (insuranceProfile != null)
                {
                    _repoWrapper.InsuranceProfile.Delete(insuranceProfile);
                    await _repoWrapper.Save();
                    return new ResponseMessage { Message = "Family Member profile was removed successfully", Status = true };
                }
                return new ResponseMessage { Message = "Family Member does not exist under your current profile" };
            }
            return new ResponseMessage { Message = "Family Profile does not exist" };
           
        }

        public void SendPaymentReminder(string email, string familyHead,string familyMember, PerformContext context)
        {
            _emailSender.SendHealthInsuredFamilyPaymentReminder(email, "HealthInsured Payment Reminder", familyHead, familyMember);
        }
    }
}
