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
using System.Security.Claims;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http;
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
using Application.ViewModels.HealthInsured;
using Infrastructure.UploadService;
using DataAccess;
using Application.DTO.HealthInsured_AxaMansard;
using Domain.Models.Axa.Hygeia_Insurance;
using Domain.Models.Axa_Hygeia_Insurance;
using Application.Services.HealthInsured_AxaMansard.Insurance;
using Application.HealthInsured_AxaMansard_Service.Insurance;
using ClosedXML.Excel;
using Application.Services.HealthInsured.Insurance;

namespace HealthBanc.Controllers.Insurance
{
    [Route("v1/api/[controller]")]
    [ApiController]
    public class InsuranceController : ControllerBase
    {
        private readonly InsuranceService _insuranceService;
        private readonly IMapper _mapper;
        private readonly IRepositoryWrapper _repoWerapper;
        private readonly AuditLogService _auditLogServices;
        private readonly IHttpContextAccessor _accessor;
        private readonly CorporateInsuranceService _corporateInsuranceService;
        public string IpAddress;
        public StringValues agent;

        public InsuranceController(InsuranceService insuranceService, IMapper mapper,IRepositoryWrapper repoWerapper,AuditLogService auditLogServices,IHttpContextAccessor accessor,
            CorporateInsuranceService corporateInsuranceService)
        {
            _insuranceService = insuranceService;
            _mapper = mapper;
            _repoWerapper = repoWerapper;
            _auditLogServices = auditLogServices;
            _accessor = accessor;
            _corporateInsuranceService = corporateInsuranceService;
            IpAddress = accessor.HttpContext.Connection.RemoteIpAddress.ToString();
            agent = accessor.HttpContext.Request.Headers["User-Agent"];
        }

        /// <summary>
        /// Get State in Nigeria
        /// </summary>
        /// <returns></returns>
        [HttpGet("[action]")]
        [Authorize(Roles = "SuperAdmin,Super-Administrator")]
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
        [Authorize(Roles = "SuperAdmin,Super-Administrator")]
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
        [Authorize(Roles = "SuperAdmin,Super-Administrator")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<List<CityListDTO>>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        public IActionResult GetTowns(string state,string insurancePovider)
        {
            if(string.IsNullOrEmpty(state) || String.IsNullOrEmpty(insurancePovider))
            {
                return BadRequest(new ResponseMessage { Status = false, Message="State or insurance provider cannot be null"});
            }
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
        [Authorize(Roles = "SuperAdmin,Super-Administrator")]
        public async Task<IActionResult> GetHealthProvider(string state, string city, string insuranceProvider)
        {
            if (string.IsNullOrEmpty(state) || String.IsNullOrEmpty(insuranceProvider) || String.IsNullOrEmpty(city))
            {
                return BadRequest(new ResponseMessage { Status = false, Message = "State or insurance provider cannot be null" });
            }
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
        [Authorize(Roles = "SuperAdmin,Super-Administrator")]
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
        [Authorize(Roles = "SuperAdmin,Super-Administrator")]
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
        /// Get paginated list of all users insurance profile. Can only be accessed by the application admins
        /// </summary>
        /// <param name="paginationQuery"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<PagedResponse<IndividualProfileDTO>>))]
        public async Task<IActionResult> GetPaginatedInsuranceProfiles([FromQuery]PaginationQuery paginationQuery)
        {
            var insuranceProfiles = await _insuranceService.GetPaginatedInsuranceProfiles(paginationQuery);
            return Ok(insuranceProfiles);
        }

        /// <summary>
        /// Get paginated transaction log of all users or specific user by specifying email. Can only be accessed by the application admins
        /// </summary>
        /// <param name="paginationQuery"></param>
        /// <param name="email"></param> 
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<PagedResponse<TransactionLogDTO>>))]
        public async Task<IActionResult> GetPaginatedTransactionLogs([FromQuery] PaginationQuery paginationQuery,string email)
        {
            var transLogs = await _insuranceService.GetPaginatedTransactionLogs(paginationQuery,email);

            return Ok(transLogs);
        }

        /// <summary>
        /// Get extended information on users insurance profile
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<IndividualProfileDTO>))]
        public async Task<IActionResult> GetExtendedInsuranceProfileByEmail(string email)
        {
            if(email != null)
            {
                var profileDetails =  await _insuranceService.GetExtendedInsuranceProfileDetailByEmail(email);
                if (!profileDetails.Status) return NotFound(profileDetails);
                return Ok(profileDetails);
            }
            return BadRequest(new ResponseMessage { Message = "Email Cannot be null", Status = false });
        }

        /// <summary>
        /// Get Logged in User Individual Insurance profile details
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
            var profile = await _repoWerapper.InsuranceProfile.GetByUserIdAsync(Id);
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
                int id = int.Parse(userId);

                var updateResponse = await _insuranceService.UpdateProfileAsync(updateProfileViewModel, id);
                if (updateResponse.Status)
                {
                    return Ok(updateResponse);
                }
                return BadRequest(updateResponse);
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
        /// Upload Axamansard Hospital List
        /// </summary>
        /// <param name="file"></param>
        /// <param name="passcode"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "Super-Administrator")]
        public async Task<IActionResult> UploadAxamansardHospitalListToDb(IFormFile file,string passcode)
        {
            if(passcode == "docUpload1963.")
            {
                var checkRole = User.IsInRole("Super-Administrator");
                if (checkRole)
                {
                    var result = await _insuranceService.UploadAxaHospitalListFromExcel(file);
                    if (result.Status)
                    {
                        return Ok(result);
                    }
                    return BadRequest();
                }
                return BadRequest(new ResponseMessage { Message = "You dont have permission to access this resource. Request for permission." });
            }            
            return BadRequest(new ResponseMessage { Message="Wrong passcode"});
        }

        /// <summary>
        /// Upload Hygeia hospital List
        /// </summary>
        /// <param name="file"></param>
        /// <param name="passcode"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "Super-Administrator")]
        public async Task<IActionResult> UploadHygeiaHospitalListToDb(IFormFile file,string passcode)
        {
            if (passcode == "docUpload1963.")
            {
                var checkRole = User.IsInRole("Super-Administrator");
                if (checkRole)
                {
                    var result = await _insuranceService.UploadHygeiaHospitalListFromExcel(file);
                    if (result.Status)
                    {
                        return Ok(result);
                    }
                    return BadRequest();
                }
                return BadRequest(new ResponseMessage { Message = "You dont have permission to access this resource. Request for permission." });
            }
            return BadRequest(new ResponseMessage { Message = "Wrong passcode" });
        }
        
        /// <summary>
        /// Generate Unique identifier
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        [Authorize(Roles = "Super-Administrator")]
        [ProducesResponseType(200, Type = typeof(File))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        public IActionResult DownloadIdentifiers(int count)
        {
            var checkRole = User.IsInRole("Super-Administrator");
            if (checkRole)
            {
                if (count < 1) return BadRequest(new ResponseMessage { Message = "Count has to be larger than zero" });
                if (count >150) return BadRequest(new ResponseMessage { Message = "Count has to be less than 150" });

                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                string fileName = "identifiers.xlsx";
                using (var workbook = new XLWorkbook())
                {
                    IXLWorksheet worksheet =
                    workbook.Worksheets.Add("Identifiers");
                    worksheet.Cell(2, 1).Value = "Id";
                    worksheet.Cell(2, 2).Value = "Unique Identifier";

                    for (int index = 2; index <= count+1; index++)
                    {
                        worksheet.Cell(index, 1).Value = index - 1;
                        worksheet.Cell(index, 2).Value = _insuranceService.GetUniqueCode();
                        worksheet.Cell(index, 2).DataType = XLDataType.Text;
                    }
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();
                        return File(content, contentType, fileName);
                    }
                }
            }
            return BadRequest(new ResponseMessage { Message = "You do not have permission to access this resource" });
        }

        /// <summary>
        /// Download insurance profile data
        /// </summary>
        /// <param name="subStatus"></param>
        /// <param name="activeStatus"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        [ProducesResponseType(200, Type = typeof(File))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        public IActionResult DowloadInsuranceProfileExcelData(bool subStatus, bool activeStatus, string service)
        {
            var response = _insuranceService.DowloadInsuranceProfileExcelData(subStatus, activeStatus, service);
            if (response.Status)
            {
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                string fileName = "HealthInsurance.xlsx";

                var content = response.Data as byte[];
                return File(content, contentType, fileName);
            }
            return BadRequest(response);
        }
    }
}
