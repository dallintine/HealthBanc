using Application.AuditAndReport.AuditLog;
using Application.DTO;
using Application.DTO.HealthInsured_AxaMansard;
using Application.HealthInsured_AxaMansard_Service.Insurance;
using Application.Services.HealthInsured.Insurance;
using Application.ViewModels.HealthInsured;
using AutoMapper;
using DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HealthBanc.Controllers.Insurance
{
    [Route("v{version:apiVersion}/api/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class CorporateInsuranceController : ControllerBase
    {
        private readonly CorporateInsuranceService _corporateInsuranceService;

        public CorporateInsuranceController(CorporateInsuranceService corporateInsuranceService)
        {
            _corporateInsuranceService = corporateInsuranceService;
        }

        /// <summary>
        /// Create Corporate insurance 
        /// </summary>
        /// <param name="corporateRegViewModel"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> CreateCorporateUser(CorporateRegistrationViewModel corporateRegViewModel)
        {
            if (ModelState.IsValid)
            {
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int Id = int.Parse(userId);
                var corporateRegistration = await _corporateInsuranceService.CreateCorporateUser(corporateRegViewModel, Id);
                if (corporateRegistration.Status)
                {
                    return Ok(corporateRegistration);
                }
                return BadRequest(corporateRegistration);
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
        /// Confirm Otp for corporate insurance registration
        /// </summary>
        /// <param name="otp"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> ConfirmOtp(string otp)
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);
            var confirmOtp = await _corporateInsuranceService.ConfirmOtp(otp, Id);
            if (confirmOtp.Status)
            {
                return Ok(confirmOtp);
            }
            return BadRequest(confirmOtp);
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
            if (resendOtp.Status)
            {
                return Ok(resendOtp);
            }
            return BadRequest(resendOtp);
        }

        /// <summary>
        /// Update Corporate insurance details
        /// </summary>
        /// <param name="updateCorporateUserViewModel"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<CompanyProfileDTO>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateCorporateUser(UpdateCorporateUserViewModel updateCorporateUserViewModel)
        {
            if (ModelState.IsValid)
            {
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int Id = int.Parse(userId);
                var corporateUser = await _corporateInsuranceService.UpdateCorporateUser(updateCorporateUserViewModel, Id);
                return Ok(corporateUser);
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
        /// Upload excel file containing insurance details for users under a corporate organization.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<PagedResponse<BeneficiaryReviewDTO>>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public async Task<IActionResult> UploadUserProfileFromExcelFile([FromForm]UploadViewModel file)
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
        /// Get company Beneficiary Review
        /// </summary>
        /// <param name="paginationQuery"></param>
        /// <returns></returns>
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<PagedResponse<BeneficiaryReviewDTO>>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public async Task<IActionResult> GetCompanyBeneficiaryReview([FromQuery] PaginationQuery paginationQuery)
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            var beneficiariesResponse = await _corporateInsuranceService.GetBeneficiariesReview(paginationQuery, Id);
            if (beneficiariesResponse.Status)
            {
                return Ok(beneficiariesResponse);
            }
            else
            {
                return BadRequest(beneficiariesResponse);
            }
        }

        /// <summary>
        /// Remove company beneficiary Review user after user upload excel document
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        public async Task<IActionResult> RemoveCompanyBeneficiaryReviewUser(string email)
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            var response = await _corporateInsuranceService.RemoveCompanyBeneficiary(email, Id);
            if (response.Status)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        /// <summary>
        /// Restore company beneficiary review user after user upload excel documnet
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        public async Task<IActionResult> RestoreCompanyBeneficiaryReviewUser(string email)
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            var response = await _corporateInsuranceService.RestoreCompanyBeneficiary(email, Id);
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
        public async Task<IActionResult> MoveCompanyBeneficiaryFromPendingToInactiveState(BeneficiaryListViewModel beneficiaryListViewModel)
        {
            if (ModelState.IsValid)
            {
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int Id = int.Parse(userId);

                if (beneficiaryListViewModel.Emails.Count < 1)
                {
                    return BadRequest(new ResponseMessage { Message = "No option was selected", Status = false });
                }

                var corporateUser = await _corporateInsuranceService.MoveCompanyBeneficiaryFromPendingToInactiveState(beneficiaryListViewModel, Id);
                return Ok(corporateUser);
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
        ///Move List of company beneficiaries from inactive state to pending state
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public async Task<IActionResult> MoveCompanyBeneficiaryFromInactiveToPendingState(BeneficiaryListViewModel beneficiaryListViewModel)
        {
            if (ModelState.IsValid)
            {
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int Id = int.Parse(userId);

                if (beneficiaryListViewModel.Emails.Count < 1)
                {
                    return BadRequest(new ResponseMessage { Message = "No option was selected", Status = false });
                }

                var corporateUser = await _corporateInsuranceService.MoveCompanyBeneficiaryFromInactiveToPendingState(beneficiaryListViewModel, Id);
                return Ok(corporateUser);
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
        /// Get Company insurance beneficiaries
        /// </summary>
        /// <param name="paginationQuery"></param>
        /// <returns></returns>
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<PagedResponse<InsuranceBeneficiaryDTO>>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public async Task<IActionResult> GetCompanyBeneficiaries([FromQuery]PaginationQuery paginationQuery)
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            var beneficiariesResponse = await _corporateInsuranceService.GetCompanyBeneficiaries(paginationQuery, Id);
            if (beneficiariesResponse.Status)
            {
                return Ok(beneficiariesResponse);
            }
            else
            {
                return BadRequest(beneficiariesResponse);
            }
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
            if (analytics.Status)
            {
                return Ok(analytics);
            }
            return BadRequest(analytics);
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
            if (companyProfile.Status)
            {
                return Ok(companyProfile);
            }
            return BadRequest(companyProfile);
        }
    }
}
