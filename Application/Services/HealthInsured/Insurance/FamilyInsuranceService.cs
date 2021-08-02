using Application.DTO;
using Application.ViewModels.HealthInsured;
using AutoMapper;
using DataAccess;
using Domain.Models;
using Domain.Models.Axa_Hygeia_Insurance;
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

        public FamilyInsuranceService(IRepositoryWrapper repoWrapper,IMapper mapper, UserManager<ApplicationUser> userManager)
        {
            _repoWrapper = repoWrapper;
            _mapper = mapper;
            _userManager = userManager;
        }

        public async Task<ResponseMessage> CreateFamilyProfile(int id, string phoneNumber)
        {
            var user = await _repoWrapper.ApplicationUser.FindByIdAsync(id);
            var family = _mapper.Map<FamilyProfile>(user);
            family.PhoneNumber = phoneNumber;
            _repoWrapper.FamilyProfile.Create(family);
            user.PhoneNumber = phoneNumber;
            _repoWrapper.ApplicationUser.Update(user);
            await _repoWrapper.Save();
            return new ResponseMessage { Status = true, Message = "Profile was created successfully" };
        }

        public async Task<ResponseMessage> CreateInsuranceProfileForFamilyMemeber(FamilyMemberViewModel familyMemberViewModel, int familyProfileId)
        {
            var familyCreator = await _repoWrapper.FamilyProfile.GetByUserId(familyProfileId);

            familyMemberViewModel.PlanCode = "1";
            familyMemberViewModel.Premium = decimal.Parse("1000");
            var insuranceProfile = _mapper.Map<InsuranceUserProfile>(familyMemberViewModel);
            _repoWrapper.InsuranceProfile.Create(insuranceProfile);
            await _repoWrapper.Save();
            return new ResponseMessage { Status = true, Message = "Family member was added successfully" };
        }

        //public async Task<ResponseMessage> DeactivateFamilyMember(int familyProfileId, int insuranceProfileId)
        //{
        //    var familyMember = await _repoWrapper.InsuranceProfile.GetByFamilyProfileId(familyProfileId, insuranceProfileId);
        //}
    }
}
