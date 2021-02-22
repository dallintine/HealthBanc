using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Logging;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Hangfire;
using Microsoft.Extensions.Primitives;
using Application.DTO;
using HealthBanc.DTO.ApplicationUserDTOs;
using Application.ViewModels;
using Domain.Models;
using DataAccess.HealthInsured.Interfaces;
using Application.Helpers;
using DataAccess.General.Interfaces;
using HealthBanc.DTO.HealthInsured_AxaMansard;
using Microsoft.AspNetCore.Cors;
using Application.AuditAndReport.AuditLog;
using Application.API_ResponseModel.HealthInsured;
using Application.Services.HealthInsured;
using Application.ViewModels.HealthInsured;
using DataAccess.HealthInsured_AxaMansard.Interfaces;
using Infrastructure.UploadService;
using DataAccess;
using Application.DTO.HealthInsured_AxaMansard;
using Domain.Models.Axa.Hygeia_Insurance;
using Domain.Models.AxaMansard_Insurance;

namespace HealthBanc.Controllers.Insurance
{
    [Route("v1/api/[controller]")]
    [ApiController]
    public class InsuranceController : ControllerBase
    {
        private readonly InsuranceService _insuranceService;
        private readonly IMapper _mapper;
        private readonly IInsuranceProfileRepository _insuranceProfileRepository;
        private readonly ILogger<InsuranceController> _logger;
        private readonly IApplicationUserRepository _userRepository;
        private readonly IInsuranceCompletionProfileRepository _completionRepository;
        private readonly ICompanyProfileRepository _companyProfileRepository;
        private readonly AuditLogService _auditLogServices;
        private readonly IHttpContextAccessor _accessor;
        public string IpAddress;
        public StringValues agent;

        public InsuranceController(InsuranceService insuranceService, IMapper mapper, IInsuranceProfileRepository insuranceProfileRepository, ILogger<InsuranceController> logger,
            IApplicationUserRepository userRepository, IInsuranceCompletionProfileRepository completionRepository, AuditLogService auditLogServices,
            IHttpContextAccessor accessor, ICompanyProfileRepository companyProfileRepository)
        {
            _insuranceService = insuranceService;
            _mapper = mapper;
            _insuranceProfileRepository = insuranceProfileRepository;
            _logger = logger;
            _userRepository = userRepository;
            _completionRepository = completionRepository;
            _auditLogServices = auditLogServices;
            _accessor = accessor;
            IpAddress = accessor.HttpContext.Connection.RemoteIpAddress.ToString();
            agent = accessor.HttpContext.Request.Headers["User-Agent"];
            _companyProfileRepository = companyProfileRepository;
        }

        /// <summary>
        /// Get State in Nigeria
        /// </summary>
        /// <returns></returns>
        [HttpGet("[action]")]
        [ProducesResponseType(200, Type = typeof(List<string>))]
        public List<string> GetState()
        {
            var stateList = new List<string>()

            { "Abia","Abuja","Adamawa","AkwaIbom","Anambra","Bauchi","Bayelsa","Benue","Borno","Cross River","Delta","Ebonyi","Edo","Ekiti",
             "Enugu","Gombe","Imo","Jigawa","Kaduna","Kano","Katsina","Kebbi","Kogi","Kwara","Lagos","Nasarawa","Niger",
             "Ogun","Ondo","Osun","Oyo","Plateau","Rivers","Sokoto","Taraba","Yobe","Zamfara"
            };
            return stateList;
        }

        /// <summary>
        /// Create Insurance profile for indivivuals. hygeia or axamansard is passed as insurance provider in the model
        /// </summary>
        /// <param name="userProfile"></param>
        /// <returns></returns>
        [EnableCors("Cors")]
        [HttpPost("[action]")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> CreateUserInsuranceProfile([FromForm] UserProfileviewModel userProfile)
        {
            if (ModelState.IsValid)
            {
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int Id = int.Parse(userId);
                var device = _auditLogServices.GetDevice(agent);

                var creatProfileResponse = await _insuranceService.UserOnboarding(userProfile, Id, IpAddress, device);

                if (creatProfileResponse.Status)
                {
                    return Ok(creatProfileResponse);
                }
                return BadRequest(creatProfileResponse);
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
        /// Get Towns with HMO coverge with state and insurance provider.insurance provider is either hygeia or axamansard 
        /// </summary>
        /// <param name="state"></param>
        /// <param name="insurancePovider"></param> 
        /// <returns></returns>
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<List<CityListDTO>>))]
        [HttpGet("[action]")]
        public IActionResult GetTowns(string state,string insurancePovider)
        {
            var townList = _insuranceService.GetTowns(state, insurancePovider);
            return Ok(townList);
        }

        /// <summary>
        /// Get Health care providers based on state,city and insurance provider.Insurance provider is either hygeia or axamansard 
        /// </summary>
        /// <param name="state"></param>
        /// <param name="city"></param>
        /// <param name="insuranceProvider"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<List<AxaMansardHospitalList>>))]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetHealthProvider(string state, string city, string insuranceProvider)
        {
            var healthProvider = await _insuranceService.GetHealthProvider(state, city, insuranceProvider);
            return Ok(healthProvider);
        }

        /// <summary>
        ///  Get filtered hygeia healthcare provider
        /// </summary>
        /// <param name="paginationQuery"></param>
        /// <param name="state"></param>
        /// <param name="city"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<PagedResponse<HygeiaHospitalList>>))]
        //[Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> FilterHygeiaHealthCareProvider([FromQuery]PaginationQuery paginationQuery,string state, string city)
        {
            var FilterHealthCareProvider = await _insuranceService.FilterHealthCareProvider(paginationQuery, state, city);
            return Ok(FilterHealthCareProvider);
        }

        /// <summary>
        /// Get Health insurance plans
        /// </summary>
        /// <returns></returns>
        [HttpGet("[action]")]
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<AxaListResponseRoot>))]
        public IActionResult AxaMansardGetHealthPlans()
        {
            var axaListResponse = new List<AxaListResponse>();           
            var axaResponse = new AxaListResponse()
            {
                Text = "Rugby",
                Code = "7"
            };
            axaListResponse.Add(axaResponse);
            var axaResponse2 = new AxaListResponse()
            {
                Text = "Sapphire",
                Code = "8"
            };
            axaListResponse.Add(axaResponse2);
            return Ok(new ResponseMessage<List<AxaListResponse>> { Data = axaListResponse, Message = "HealthPan was fetched successfully", Status = true });            
        }

        /// <summary>
        /// Get Individual Insurance profile details
        /// </summary>
        /// <returns></returns>
        [HttpGet("[action]")]
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<IndividualProfileDTO>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        public async Task<IActionResult> GetUserInsuranceProfile()
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);
            var profile = await _insuranceProfileRepository.GetByUserIdAsync(Id);
            if(profile != null)
            {
                var profileDTO = _mapper.Map<IndividualProfileDTO>(profile);
                return Ok(new ResponseMessage {Data= profileDTO, Message="User profile was fetched successfully",Status=true });
            }
            return BadRequest(new ResponseMessage { Message = "Profile was not found" });
        }

        /// <summary>
        /// Get healthinsured completion profile details
        /// </summary>
        /// <returns></returns>
        [HttpGet("[action]")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<HealthInsuredProfileStateDTO>))]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetProfileCompletion()
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            var profileCompletion = await _insuranceService.GetProfileCompletion(Id);
            return Ok(profileCompletion);
        }

        /// <summary>
        /// Update individual insurance profile
        /// </summary>
        /// <param name="updateProfileViewModel"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateProfileAsync(UpdateProfileViewModel updateProfileViewModel)
        {
            if (ModelState.IsValid)
            {
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int Id = int.Parse(userId);
                var checkIfUserHasBeenProfiled = await _insuranceProfileRepository.GetByUserIdAsync(Id);
                if (checkIfUserHasBeenProfiled == null) return BadRequest(new ResponseMessage { Message = "User does not have a profile" });

                var updatedProfile = _mapper.Map(updateProfileViewModel, checkIfUserHasBeenProfiled);
                try
                {
                    updatedProfile.CareProviderName = updateProfileViewModel.CareProviderName.Split(':')[0];
                    updatedProfile.CPAddress = updateProfileViewModel.CareProviderName.Split(':')[1];
                    updatedProfile.CPCity = updateProfileViewModel.CareProviderName.Split(":").Length == 3 ? updateProfileViewModel.CareProviderName.Split(":")[2] : "";
                }
                catch (Exception ex)
                {
                    updatedProfile.CareProviderName = updateProfileViewModel.CareProviderName;
                }
                _insuranceProfileRepository.Update(updatedProfile);
                await _insuranceProfileRepository.Save();

                var auditViewModel = new AuditLogViewModel(Id, null, null, "Updated HealthInsured Profile", "Updated HealthInsured profile");
                var device = _auditLogServices.GetDevice(agent);
                BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel,IpAddress,device));
                return Ok(new ResponseMessage { Status = true, Message = "Profile was updated successfully" });

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
                var corporateRegistration = await _insuranceService.CreateCorporateUser(corporateRegViewModel, Id);
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
            var confirmOtp = await _insuranceService.ConfirmOtp(otp, Id);
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
            var resendOtp = await _insuranceService.ResendOtp(Id);
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
                var corporateUser = await _insuranceService.UpdateCorporateUser(updateCorporateUserViewModel, Id);
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
        /// Uploaded details create CompanyInsuranceUsers
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [Authorize]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<PagedResponse<CompanyInsuranceUser>>))]
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

            var response = await _insuranceService.UploadUserProfileFromExcelFile(file.FileUpload, Id);
            if (response.Status)
            {
                return Ok(response);
            }
            return BadRequest(response);  
        }

        /// <summary>
        /// Remove company beneficiary after user upload excel document
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [Authorize]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        public async Task<IActionResult> RemoveCompanyBeneficiary(string email)
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            var response = await _insuranceService.RemoveCompanyBeneficiary(email, Id);
            if (response.Status)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        /// <summary>
        /// Restore company beneficiary after user upload excel documnet
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [Authorize]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        public async Task<IActionResult> RestoreCompanyBeneficiary(string email)
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            var response = await _insuranceService.RemoveCompanyBeneficiary(email, Id);
            if (response.Status)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        /// <summary>
        /// Get Company insurance beneficiaries
        /// </summary>
        /// <param name="paginationQuery"></param>
        /// <returns></returns>
        [Authorize]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<PagedResponse<InsuranceBeneficiaryDTO>>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public async Task<IActionResult> GetCompanyBeneficiaries([FromQuery]PaginationQuery paginationQuery)
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            var beneficiariesResponse = await _insuranceService.GetCompanyBeneficiaries(paginationQuery, Id);
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
        [Authorize]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<CompanyProfileBeneficiaryAnalyticDTO>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        public async Task<IActionResult> CorporateSubscribersAnalytics()
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            var analytics = await _insuranceService.CorporateSubscribersAnalytics(Id);
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
        [Authorize]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<CompanyProfileDTO>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        public async Task<IActionResult> GetCompanyProfileDetails()
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            var companyProfile = await _insuranceService.GetCompanyProfileDetails(Id);
            if (companyProfile.Status)
            {
                return Ok(companyProfile);
            }
            return BadRequest(companyProfile);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> UploadAxamansardHospitalListToDb(IFormFile file,string passcode)
        {
            if(passcode == "docUpload1963.")
            {
                var result = await _insuranceService.UploadAxaHospitalListFromExcel(file);
                if (result.Status)
                {
                    return Ok(result);
                }
                return BadRequest();
            }            
            return BadRequest(new ResponseMessage { Message="Wrong passcode"});
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> UploadHygeiaHospitalListToDb(IFormFile file,string passcode)
        {
            if (passcode == "docUpload1963.")
            {
                var result = await _insuranceService.UploadHygeiaHospitalListFromExcel(file);
                if (result.Status)
                {
                    return Ok(result);
                }
                return BadRequest();
            }
            return BadRequest(new ResponseMessage { Message = "Wrong passcode" });
        }
    }
}
