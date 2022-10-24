using Application.DTO;
using Application.DTO.HealthInsured_AxaMansard;
using Application.Interfaces;
using Application.Services;
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
        private readonly ResponseHelper _responseHelper;

        public FamilyInsuranceController(IRepositoryWrapper repoWrapper,FamilyInsuranceService familyInsuranceService, IEncryptAndDecrypt encryptDecrypt
            ,ResponseHelper responseHelper )
        {
            _repoWrapper = repoWrapper;
            _familyInsuranceService = familyInsuranceService;
            _encryptDecrypt = encryptDecrypt;
            _responseHelper = responseHelper;
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
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var model = JsonConvert.DeserializeObject<CreateFamilyProfileViewModel>(decryptedString.Item2);

            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int id = int.Parse(userId);
            var createFamilyProfileResponse = await _familyInsuranceService.CreateFamilyProfile(id, model.InsuranceService);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(createFamilyProfileResponse));
            return Ok(data);
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
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var familyMemberViewModel = JsonConvert.DeserializeObject<FamilyMemberViewModel>(decryptedString.Item2);

            string id = User.FindFirst(ClaimTypes.Name)?.Value;
            int userId = int.Parse(id);
            var createInsuranceProfileResponse = await _familyInsuranceService.CreateInsuranceProfileForFamilyMemeber(familyMemberViewModel, userId);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(createInsuranceProfileResponse));
            if (createInsuranceProfileResponse.Status)
            {
                return Ok(data);
            }
            return BadRequest(data);
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
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var paginationQuery = JsonConvert.DeserializeObject<PaginationQuery>(decryptedString.Item2);

            string id = User.FindFirst(ClaimTypes.Name)?.Value;
            int userId = int.Parse(id);
            var createInsuranceProfileResponse = await _familyInsuranceService.GetFamilyMembers(paginationQuery, userId);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(createInsuranceProfileResponse));
            if (createInsuranceProfileResponse.Status)
            {
                return Ok(data);
            }
            return BadRequest(data);
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
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var model = JsonConvert.DeserializeObject<RemoveFamilyMemberViewModel>(decryptedString.Item2);

            string id = User.FindFirst(ClaimTypes.Name)?.Value;
            int userId = int.Parse(id);

            var response = await _familyInsuranceService.RemoveFamilyMember(userId, model.FamiyMemeberId);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(response));
            if (response.Status)
            {
                return Ok(data);
            }
            return NotFound(data);
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
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var model = JsonConvert.DeserializeObject<UpdateFamilyMemberProfileViewModel>(decryptedString.Item2);

            string id = User.FindFirst(ClaimTypes.Name)?.Value;
            int userId = int.Parse(id);

            var response = await _familyInsuranceService.UpdateFamilyMember(userId, model.FamilyMemberId, model);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(response));
            if (response.Status)
            {
                return Ok(data);
            }
            return NotFound(data);
        }

    }
}
