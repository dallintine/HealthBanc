using Application.DTO;
using Application.DTO.HealthInsured_AxaMansard;
using Application.Services.HealthInsured.Insurance;
using Application.ViewModels.HealthInsured;
using DataAccess;
using Microsoft.AspNetCore.Authorization;
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
        [HttpGet("[action]")]
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        public async Task<IActionResult> CreateFamilyProfile()
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int id = int.Parse(userId);
            var createFamilyProfileResponse = await _familyInsuranceService.CreateFamilyProfile(id);
            return Ok(createFamilyProfileResponse);
        }

        /// <summary>
        /// Add a new user to the family member plan
        /// </summary>
        /// <param name="familyMemberViewModel"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        public async Task<IActionResult> CreateInsuranceProfileForFamilyMember([FromBody]FamilyMemberViewModel familyMemberViewModel)
        {
            if (ModelState.IsValid)
            {
                string id = User.FindFirst(ClaimTypes.Name)?.Value;
                int userId = int.Parse(id);
                var createInsuranceProfileResponse = await _familyInsuranceService.CreateInsuranceProfileForFamilyMemeber(familyMemberViewModel, userId);
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
        
        /// <summary>
        /// Get Paginated family members data.
        /// </summary>
        /// <param name="paginationQuery"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<PagedResponse<FamilyMembersDTO>>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetFamilyMembers([FromQuery] PaginationQuery paginationQuery)
        {
            if (ModelState.IsValid)
            {
                string id = User.FindFirst(ClaimTypes.Name)?.Value;
                int userId = int.Parse(id);
                var createInsuranceProfileResponse = await _familyInsuranceService.GetFamilyMembers(paginationQuery, userId);
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

        /// <summary>
        /// Remove family member that has not been activated
        /// </summary>
        /// <param name="famiyMemeberId"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage))]
        public async Task<IActionResult> RemoveFamilyMember(int famiyMemeberId)
        {
            string id = User.FindFirst(ClaimTypes.Name)?.Value;
            int userId = int.Parse(id);

            var response = await _familyInsuranceService.RemoveFamilyMember(userId, famiyMemeberId);
            if (response.Status)
            {
                return Ok(response);
            }
            return NotFound(response);
        }

        /// <summary>
        /// Update family member that has not been activated
        /// </summary>
        /// <param name="famiyMemeberId"></param>
        /// <param name="familyMemberViewModel"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage))]
        public async Task<IActionResult> UpdateFamilyMemberProfile(int famiyMemeberId,[FromBody]FamilyMemberViewModel familyMemberViewModel)
        {
            string id = User.FindFirst(ClaimTypes.Name)?.Value;
            int userId = int.Parse(id);

            var response = await _familyInsuranceService.UpdateFamilyMember(userId, famiyMemeberId, familyMemberViewModel);
            if (response.Status)
            {
                return Ok(response);
            }
            return NotFound(response);
        }

    }
}
