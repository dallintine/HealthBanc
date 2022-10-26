using Application.DTO;
using Application.DTO.HealthInsured_AxaMansard;
using Application.Interfaces;
using Application.Services.HealthInsured.Insurance;
using Application.ViewModels.HealthInsured;
using DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HealthBanc.Controllers.V2.Insurance
{
    [Route("v{version:apiVersion}/api/[controller]")]
    [ApiController]
    [ApiVersion("2.0")]
    public class FamilyInsuranceController : ControllerBase
    {
        private readonly IRepositoryWrapper _repoWrapper;
        private readonly FamilyInsuranceService _familyInsuranceService;
        private readonly IEncryptAndDecrypt _encryptDecrypt;

        public FamilyInsuranceController(IRepositoryWrapper repoWrapper,FamilyInsuranceService familyInsuranceService, IEncryptAndDecrypt encryptDecrypt)
        {
            _repoWrapper = repoWrapper;
            _familyInsuranceService = familyInsuranceService;
            _encryptDecrypt = encryptDecrypt;
        }

        /// <summary>
        /// Onboard user to the healthinsured family plan
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        public async Task<IActionResult> CreateFamilyProfile(EncryptedModel encryptedModel)
        {
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 });
            var model = JsonConvert.DeserializeObject<CreateFamilyProfileViewModel>(decryptedString.Item2);

            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int id = int.Parse(userId);
            var createFamilyProfileResponse = await _familyInsuranceService.CreateFamilyProfile(id, model.InsuranceService);
            return Ok(createFamilyProfileResponse);
        }

        /// <summary>
        /// Add a new user to the family member plan
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        public async Task<IActionResult> CreateInsuranceProfileForFamilyMember(EncryptedModel encryptedModel)
        {
            if (ModelState.IsValid)
            {
                var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
                if (!decryptedString.Item1) return BadRequest(new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 });
                var familyMemberViewModel = JsonConvert.DeserializeObject<FamilyMemberViewModel>(decryptedString.Item2);

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
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<PagedResponse<FamilyMembersDTO>>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetFamilyMembers(EncryptedModel encryptedModel)
        {
            if (ModelState.IsValid)
            {
                var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
                if (!decryptedString.Item1) return BadRequest(new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 });
                var paginationQuery = JsonConvert.DeserializeObject<PaginationQuery>(decryptedString.Item2);

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
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage))]
        public async Task<IActionResult> RemoveFamilyMember(EncryptedModel encryptedModel)
        {
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 });
            var model = JsonConvert.DeserializeObject<RemoveFamilyMemberViewModel>(decryptedString.Item2);

            string id = User.FindFirst(ClaimTypes.Name)?.Value;
            int userId = int.Parse(id);

            var response = await _familyInsuranceService.RemoveFamilyMember(userId, model.FamiyMemeberId);
            if (response.Status)
            {
                return Ok(response);
            }
            return NotFound(response);
        }

        /// <summary>
        /// Update family member that has not been activated
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage))]
        public async Task<IActionResult> UpdateFamilyMemberProfile(EncryptedModel encryptedModel)
        {
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 });
            var model = JsonConvert.DeserializeObject<UpdateFamilyMemberProfileViewModel>(decryptedString.Item2);

            string id = User.FindFirst(ClaimTypes.Name)?.Value;
            int userId = int.Parse(id);

            var response = await _familyInsuranceService.UpdateFamilyMember(userId, model.FamilyMemberId, model);
            if (response.Status)
            {
                return Ok(response);
            }
            return NotFound(response);
        }

    }
}
