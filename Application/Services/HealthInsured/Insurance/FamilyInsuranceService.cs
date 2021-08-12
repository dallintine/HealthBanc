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

        public async Task<ResponseMessage> CreateFamilyProfile(int id, string phoneNumber)
        {
            var user = await _repoWrapper.ApplicationUser.FindByIdAsync(id);
            var family = _mapper.Map<FamilyProfile>(user);
            family.PhoneNumber = phoneNumber;
            family.ProfileCompleted = true;
            _repoWrapper.FamilyProfile.Create(family);
            user.PhoneNumber = phoneNumber;
            _repoWrapper.ApplicationUser.Update(user);
            await _repoWrapper.Save();
            return new ResponseMessage { Status = true, Message = "Profile was created successfully" };
        }

        public async Task<ResponseMessage> CreateInsuranceProfileForFamilyMemeber(FamilyMemberViewModel familyMemberViewModel, int userId)
        {
            var familyCreator = await _repoWrapper.FamilyProfile.GetByUserId(userId);
            familyMemberViewModel.PlanCode = "1";
            familyMemberViewModel.Premium = decimal.Parse("1000");
            var insuranceProfile = _mapper.Map<InsuranceUserProfile>(familyMemberViewModel);
            insuranceProfile.FamilyProfileId = familyCreator.Id;
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

        public void SendPaymentReminder(string email, string familyHead,string familyMember, PerformContext context)
        {
            _emailSender.SendHealthInsuredFamilyPaymentReminder(email, "HealthInsured Payment Reminder", familyHead, familyMember);
        }
    }
}
