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
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HealthBanc.Controllers.V2.Insurance
{
    [Route("v{version:apiVersion}/api/[controller]")]
    [ApiController]
    [ApiVersion("2.0")]
    public class CorporateInsuranceController : ControllerBase
    {
        private readonly CorporateInsuranceService _corporateInsuranceService;
        private readonly IEncryptAndDecrypt _encryptDecrypt;
        private readonly ResponseHelper _responseHelper;

        public CorporateInsuranceController(CorporateInsuranceService corporateInsuranceService, IEncryptAndDecrypt encryptDecrypt,ResponseHelper responseHelper)
        {
            _corporateInsuranceService = corporateInsuranceService;
            _encryptDecrypt = encryptDecrypt;
            _responseHelper = responseHelper;
        }

        /// <summary>
        /// Create Corporate insurance 
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> CreateCorporateUser(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var corporateRegViewModel = JsonConvert.DeserializeObject<CorporateRegistrationViewModel>(decryptedString.Item2);

            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);
            var corporateRegistration = await _corporateInsuranceService.CreateCorporateUser(corporateRegViewModel, Id);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(corporateRegistration));
            if (corporateRegistration.Status)
            {
                return Ok(data);
            }
            return BadRequest(data);
        }
               

        /// <summary>
        /// Resend otp for corporate insurance registration
        /// </summary>
        /// <returns></returns>
        [HttpGet("[action]")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> ResendOTP()
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);
            var resendOtp = await _corporateInsuranceService.ResendOtp(Id);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(resendOtp));
            if (resendOtp.Status)
            {
                return Ok(data);
            }
            return BadRequest(data);
        }

        /// <summary>
        /// Update Corporate insurance details
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<CompanyProfileDTO>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateCorporateUser(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var updateCorporateUserViewModel = JsonConvert.DeserializeObject<UpdateCorporateUserViewModel>(decryptedString.Item2);

            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);
            var corporateUser = await _corporateInsuranceService.UpdateCorporateUser(updateCorporateUserViewModel, Id);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(corporateUser));
            return Ok(data);
        }

        /// <summary>
        /// Upload excel file containing insurance details for users under a corporate organization.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<PagedResponse<BeneficiaryReviewDTO>>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public async Task<IActionResult> UploadUserProfileFromExcelFile([FromForm] UploadViewModel file)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            var response = await _corporateInsuranceService.UploadUserProfileFromExcelFile(file.FileUpload, Id);
            if (response.Status)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        /// <summary>
        ///Move List of company beneficiaries from pending state to inactive state
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public async Task<IActionResult> MoveCompanyBeneficiaryFromPendingToInactiveState(EncryptedModel encryptedModel)
        {  
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var beneficiaryListViewModel = JsonConvert.DeserializeObject<BeneficiaryListViewModel>(decryptedString.Item2);

            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            if (beneficiaryListViewModel.Emails.Count < 1)
            {
                var data2 = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(new ResponseMessage { Message = "No option was selected", Status = false }));
                return BadRequest(data2);
            }

            var corporateUser = await _corporateInsuranceService.MoveCompanyBeneficiaryFromPendingToInactiveState(beneficiaryListViewModel, Id);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(corporateUser));
            return Ok(data);
        }

        /// <summary>
        ///Move List of company beneficiaries from inactive state to pending state
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public async Task<IActionResult> MoveCompanyBeneficiaryFromInactiveToPendingState(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var beneficiaryListViewModel = JsonConvert.DeserializeObject<BeneficiaryListViewModel>(decryptedString.Item2);

            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            if (beneficiaryListViewModel.Emails.Count < 1)
            {
                var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(new ResponseMessage { Message = "No option was selected", Status = false }));
                return BadRequest(data);
            }

            var corporateUser = await _corporateInsuranceService.MoveCompanyBeneficiaryFromInactiveToPendingState(beneficiaryListViewModel, Id);
            var data2 = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(corporateUser));
            return Ok(data2);
        }
              

        /// <summary>
        /// Get corporate insurance dashboard analytics
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<CompanyProfileBeneficiaryAnalyticDTO>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        public async Task<IActionResult> CorporateSubscribersAnalytics()
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            var analytics = await _corporateInsuranceService.CorporateSubscribersAnalytics(Id);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(analytics));
            if (analytics.Status)
            {
                return Ok(data);
            }
            return BadRequest(data);
        }

        /// <summary>
        /// Get corporate insurance profile details
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<CompanyProfileDTO>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        public async Task<IActionResult> GetCompanyProfileDetails()
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            var companyProfile = await _corporateInsuranceService.GetCompanyProfileDetails(Id);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(companyProfile));

            if (companyProfile.Status)
            {
                return Ok(data);
            }
            return BadRequest(data);
        }

        /// <summary>
        /// Confirm Otp for corporate insurance registration
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> ConfirmOtp(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var confirmOTP = JsonConvert.DeserializeObject<CoporateConfirmOTP>(decryptedString.Item2);

            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);
            var confirmOtp = await _corporateInsuranceService.ConfirmOtp(confirmOTP.OTP, Id);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(confirmOtp));
            if (confirmOtp.Status)
            {
                return Ok(data);
            }
            return BadRequest(data);
        }

        /// <summary>
        /// Get company Beneficiary Review
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<PagedResponse<BeneficiaryReviewDTO>>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public async Task<IActionResult> GetCompanyBeneficiaryReview(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var paginationQuery = JsonConvert.DeserializeObject<PaginationQuery>(decryptedString.Item2);

            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            var beneficiariesResponse = await _corporateInsuranceService.GetBeneficiariesReview(paginationQuery, Id);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(beneficiariesResponse));
            if (beneficiariesResponse.Status)
            {
                return Ok(data);
            }
            else
            {
                return BadRequest(data);
            }
        }

        /// <summary>
        /// Remove company beneficiary Review user after user upload excel document
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public async Task<IActionResult> RemoveCompanyBeneficiaryReviewUser(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var companyBeneficiaryReviewUser = JsonConvert.DeserializeObject<EmailViewModel>(decryptedString.Item2);

            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            var response = await _corporateInsuranceService.RemoveCompanyBeneficiary(companyBeneficiaryReviewUser.Email, Id);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(response));
            if (response.Status)
            {
                return Ok(data);
            }
            return BadRequest(data);
        }

        /// <summary>
        /// Restore company beneficiary review user after user upload excel documnet
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public async Task<IActionResult> RestoreCompanyBeneficiaryReviewUser(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var companyBeneficiaryReviewUser = JsonConvert.DeserializeObject<EmailViewModel>(decryptedString.Item2);

            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            var response = await _corporateInsuranceService.RestoreCompanyBeneficiary(companyBeneficiaryReviewUser.Email, Id);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(response));
            if (response.Status)
            {
                return Ok(data);
            }
            return BadRequest(data);
        }

        /// <summary>
        /// Get Company insurance beneficiaries
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<PagedResponse<InsuranceBeneficiaryDTO>>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public async Task<IActionResult> GetCompanyBeneficiaries(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var paginationQuery = JsonConvert.DeserializeObject<PaginationQuery>(decryptedString.Item2);

            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            var beneficiariesResponse = await _corporateInsuranceService.GetCompanyBeneficiaries(paginationQuery, Id);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(beneficiariesResponse));
            if (beneficiariesResponse.Status)
            {
                return Ok(data);
            }
            else
            {
                return BadRequest(data);
            }
        }
    }
}
