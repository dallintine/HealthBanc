using Application.DTO;
using Application.Services.HealthInsured.Insurance;
using Application.ViewModels.HealthInsured;
using DataAccess;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HealthBanc.Controllers.Insurance
{
    public class FamilyInsuranceController : ControllerBase
    {
        private readonly IRepositoryWrapper _repoWrapper;
        private readonly FamilyInsuranceService _familyInsuranceService;

        public FamilyInsuranceController(IRepositoryWrapper repoWrapper,FamilyInsuranceService familyInsuranceService)
        {
            _repoWrapper = repoWrapper;
            _familyInsuranceService = familyInsuranceService;
        }

        /// <summary>
        /// Onboard user to the healthinsured family plan
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> CreateFamilyProfile(string phoneNumber)
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int id = int.Parse(userId);
            var createFamilyProfileResponse = await _familyInsuranceService.CreateFamilyProfile(id, phoneNumber);
            return Ok(createFamilyProfileResponse);
        }

        /// <summary>
        /// Add a new user to the family member plan
        /// </summary>
        /// <param name="familyMemberViewModel"></param>
        /// <returns></returns>
        public async Task<IActionResult> CreateInsuranceProfileForFamilyMemeber(FamilyMemberViewModel familyMemberViewModel)
        {
            if (ModelState.IsValid)
            {
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int familyProfileId = int.Parse(userId);
                var createInsuranceProfileResponse = await _familyInsuranceService.CreateInsuranceProfileForFamilyMemeber(familyMemberViewModel, familyProfileId);
                if (createInsuranceProfileResponse.Status)
                {
                    return Ok(createInsuranceProfileResponse);
                }
                return BadRequest(createInsuranceProfileResponse);
            }
            //return validation errors
            var errors = new List<string>();
            var errorList = ModelState.Values.SelectMany(m => m.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            foreach (var error in errorList)
            {
                errors.Add(error);
            }
            return BadRequest(new ResponseMessage { Data = errors, Message = errors.FirstOrDefault().ToString() });
        }
    }
}
