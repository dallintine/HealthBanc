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
using DataAccess.DTO.InsuranceDTO;
using Application.Services.Card;
using Application.Interfaces;
using Application.ViewModels;
using HealthBanc.Decorators;
using Application.Services;

namespace HealthBanc.Controllers.V2.Insurance
{
    [Route("v{version:apiVersion}/api/[controller]")]
    [ApiController]
    [ApiVersion("2.0")]
    public class InsuranceController : ControllerBase
    {
        private readonly InsuranceService _insuranceService;
        private readonly IMapper _mapper;
        private readonly IRepositoryWrapper _repoWerapper;
        private readonly AuditLogService _auditLogServices;
        private readonly IEncryptAndDecrypt _encryptDecrypt;
        private readonly ResponseHelper _responsehelper;
        public string IpAddress;
        public StringValues agent;

        public InsuranceController(InsuranceService insuranceService, IMapper mapper,IRepositoryWrapper repoWerapper,AuditLogService auditLogServices,IHttpContextAccessor accessor,
             IEncryptAndDecrypt encryptDecrypt, ResponseHelper responsehelper)
        {
            _insuranceService = insuranceService;
            _mapper = mapper;
            _repoWerapper = repoWerapper;
            _auditLogServices = auditLogServices;
            _encryptDecrypt = encryptDecrypt;
            _responsehelper = responsehelper;
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
        public IActionResult GetState()
        {
            var stateList = new List<string>()

            { "Abia","Abuja","Adamawa","AkwaIbom","Anambra","Bauchi","Bayelsa","Benue","Borno","Cross River","Delta","Ebonyi","Edo","Ekiti",
             "Enugu","Gombe","Imo","Jigawa","Kaduna","Kano","Katsina","Kebbi","Kogi","Kwara","Lagos","Nasarawa","Niger",
             "Ogun","Ondo","Osun","Oyo","Plateau","Rivers","Sokoto","Taraba","Yobe","Zamfara"
            };
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(stateList));
            return Ok(data);
        }

        /// <summary>
        /// Get Towns with HMO coverge with state and insurance provider.insurance provider is either hygeia or axamansard 
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [Authorize(Roles = "SuperAdmin,Super-Administrator")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<List<CityListDTO>>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public IActionResult GetTowns(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responsehelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var model = JsonConvert.DeserializeObject<GetTownsViewModel>(decryptedString.Item2);

            if (string.IsNullOrEmpty(model.State) || String.IsNullOrEmpty(model.InsurancePovider))
            {
                return BadRequest(new ResponseMessage { Status = false, Message = "State or insurance provider cannot be null" });
            }
            var townList = _insuranceService.GetTowns(model.State, model.InsurancePovider);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(townList));
            return Ok(data);
        }

        /// <summary>
        /// Get Health care providers based on state,city and insurance provider.Insurance provider is either hygeia or axamansard 
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<List<AxaMansardHospitalList>>))]
        //[Authorize(Roles = "SuperAdmin,Super-Administrator")]
        public async Task<IActionResult> GetHealthProvider(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responsehelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var model = JsonConvert.DeserializeObject<GetHealthProviderViewModel>(decryptedString.Item2);

            if (string.IsNullOrEmpty(model.State) || String.IsNullOrEmpty(model.InsurancePovider))
            {
                return BadRequest(new ResponseMessage { Status = false, Message = "State or insurance provider cannot be null" });
            }
            var healthProvider = await _insuranceService.GetHealthProvider(model.State, null, model.InsurancePovider);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(healthProvider));
            return Ok(data);
        }

        /// <summary>
        ///  Get filtered hygeia healthcare provider
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<PagedResponse<HygeiaHospitalList>>))]
        [Authorize(Roles = "SuperAdmin,Super-Administrator")]
        public async Task<IActionResult> FilterHygeiaHealthCareProvider(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responsehelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var model = JsonConvert.DeserializeObject<FilterHygeiaHealthCareProviderModel>(decryptedString.Item2);

            var FilterHealthCareProvider = await _insuranceService.FilterHealthCareProvider(model, model.State, model.City);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(FilterHealthCareProvider));

            return Ok(data);
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
            var result = new ResponseMessage<List<AxaListResponse>> { Data = axaListResponse, Message = "HealthPan was fetched successfully", Status = true };
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(result));

            return Ok(data);
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
        public async Task<IActionResult> CreateUserInsuranceProfile([FromForm]UserProfileviewModel userProfile)
        {
            if (!ModelState.IsValid) return BadRequest(_responsehelper.BuildResponse(30, ModelState));
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);
            var device = _auditLogServices.GetDevice(agent);


            var creatProfileResponse = await _insuranceService.UserOnboarding(userProfile, Id, IpAddress, device);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(creatProfileResponse));
            if (creatProfileResponse.Status)
            {
                return Ok(data);
            }
            return BadRequest(data);
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
            if (profile != null)
            {
                var profileDTO = _mapper.Map<IndividualProfileDTO>(profile);
                var profileData = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(new ResponseMessage { Data = profileDTO, Message = "User profile was fetched successfully", Status = true }));
                return Ok(profileData);
            }
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(new ResponseMessage { Message = "Profile was not found" }));
            return BadRequest(data);
        }

        /// <summary>
        /// Get extended information on users insurance profile
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<IndividualProfileDTO>))]
        public async Task<IActionResult> GetExtendedInsuranceProfileByEmail(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responsehelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var model = JsonConvert.DeserializeObject<EmailViewModel>(decryptedString.Item2);

            if (model.Email != null)
            {
                var profileDetails = await _insuranceService.GetExtendedInsuranceProfileDetailByEmail(model.Email);
                var profileData = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(profileDetails));
                if (!profileDetails.Status) return NotFound(profileData);
                return Ok(profileData);
            }
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(new ResponseMessage { Message = "Email Cannot be null", Status = false }));
            return BadRequest(data);
        }

        /// <summary>
        /// Update individual insurance profile
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateProfileAsync(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responsehelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var updateProfileViewModel = JsonConvert.DeserializeObject<UpdateProfileViewModel>(decryptedString.Item2);

            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);
            var device = _auditLogServices.GetDevice(agent);
            int id = int.Parse(userId);

            var updateResponse = await _insuranceService.UpdateProfileAsync(updateProfileViewModel, id, IpAddress, device);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(updateResponse));

            if (updateResponse.Status)
            {
                return Ok(data);
            }
            return BadRequest(data);
        }

        /// <summary>
        /// Admin Update individual insurance profile
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "Super-Administrator")]
        public async Task<IActionResult> AdminUpdateProfileAsync(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responsehelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var model = JsonConvert.DeserializeObject<AdminUpdateProfileAsyncViewModel>(decryptedString.Item2);
            var device = _auditLogServices.GetDevice(agent);
            var insuranceProfile = await _repoWerapper.InsuranceProfile.GetByEmail(model.Email);

            var updateResponse = await _insuranceService.UpdateProfileAsync(model, insuranceProfile.UserId.Value, IpAddress, device);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(updateResponse));

            if (updateResponse.Status)
            {
                return Ok(data);
            }
            return BadRequest(data);
        }

        /// <summary>
        /// Get paginated list of all users insurance profile. Can only be accessed by the application admins
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<PagedResponse<List_IndividualProfileDTO>>))]
        public async Task<IActionResult> GetPaginatedInsuranceProfiles(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responsehelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var paginationQuery = JsonConvert.DeserializeObject<PaginationQuery>(decryptedString.Item2);

            var insuranceProfiles = await _insuranceService.GetPaginatedInsuranceProfiles(paginationQuery);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(insuranceProfiles));

            return Ok(data);
        }

        /// <summary>
        /// Pay for individual with just email details
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult>  PayforRefereeWithEmail(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responsehelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var model = JsonConvert.DeserializeObject<PayforRefereeWithEmailViewModel>(decryptedString.Item2);

            string id = User.FindFirst(ClaimTypes.Name)?.Value;
            int userId = int.Parse(id);
            var device = _auditLogServices.GetDevice(agent);

            var response = await _insuranceService.PayforNewIndividualWithEmail(userId, model.Email, model.InsuranceService, IpAddress,device);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(response));


            if (response.Status)
            {
                return Ok(data);
            }
            return BadRequest(data);
        }

        [HttpPost("[action]")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> PayForRefereeWithFullDetails([FromForm] PayForRefereeViewModel refereeViewModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responsehelper.BuildResponse(30, ModelState));

            string id = User.FindFirst(ClaimTypes.Name)?.Value;
            int userId = int.Parse(id);
            var device = _auditLogServices.GetDevice(agent);

            var response = await _insuranceService.PayForRefereeWithFullDetails(userId, refereeViewModel,IpAddress,device);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(response));


            if (response.Status)
            {
                return Ok(data);
            }
            return BadRequest(data);
        }

        /// <summary>
        /// Get insuranceProfiles of paid referees
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetPaginatedRefereeInsuranceProfiles(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responsehelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var paginationQuery = JsonConvert.DeserializeObject<PaginationQuery>(decryptedString.Item2);

            string id = User.FindFirst(ClaimTypes.Name)?.Value;
            int userId = int.Parse(id);
            var insuranceProfiles = await _insuranceService.GetReferedInsuranceProfiles(paginationQuery, userId);
            var profileData = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(insuranceProfiles));

            return Ok(profileData);            
        }

        /// <summary>
        /// Removed paid referee from list
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> RemovePaidReferee(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responsehelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var model = JsonConvert.DeserializeObject<EmailViewModel>(decryptedString.Item2);

            string id = User.FindFirst(ClaimTypes.Name)?.Value;
            int userId = int.Parse(id);
            var device = _auditLogServices.GetDevice(agent);

            var response = await _insuranceService.RemovePaidReferee(userId, model.Email,IpAddress,device);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(response));

            if (response.Status)
            {
                return Ok(data);
            }
            return BadRequest(data);
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

            var user = await _repoWerapper.ApplicationUser.FindByIdAsync(Id);
            var profileCompletion = await _insuranceService.GetProfileCompletion(Id,user.Email);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(profileCompletion));

            return Ok(data);
        }

        /// <summary>
        /// Get paginated transaction log of all users or specific user by specifying email. Can only be accessed by the application admins
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "Super-Administrator,Administrator,Technical-Support,Analyst")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<PagedResponse<TransactionLogDTO>>))]
        public async Task<IActionResult> GetPaginatedTransactionLogs(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responsehelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var model = JsonConvert.DeserializeObject<PaginatedTransactionLogsViewModel>(decryptedString.Item2);

            var transLogs = await _insuranceService.GetPaginatedTransactionLogs(model, model.Email);

            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(transLogs));

            return Ok(data);
        }

        /// <summary>
        /// Generate Unique identifier
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "Super-Administrator")]
        [ProducesResponseType(200, Type = typeof(File))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        public IActionResult DownloadIdentifiers(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responsehelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var model = JsonConvert.DeserializeObject<IdViewModel>(decryptedString.Item2);

            var count = model.Id.Value;
            var checkRole = User.IsInRole("Super-Administrator");
            if (checkRole)
            {
                if (count < 1) return BadRequest(new ResponseMessage { Message = "Count has to be larger than zero" });
                if (count >150) return BadRequest(new ResponseMessage { Message = "Count has to be less than 150" });

                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                string fileName = "identifiers.xlsx";

                var content = _insuranceService.DownloadIdentifiers(count);
                return File(content, contentType, fileName);
            }
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(new ResponseMessage { Message = "You do not have permission to access this resource" }));

            return BadRequest(data);
        }

        /// <summary>
        /// Download insurance profile data
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "Super-Administrator")]
        [ProducesResponseType(200, Type = typeof(File))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        public IActionResult DowloadInsuranceProfileExcelData(EncryptedModel encryptedModel)
        {
            if (!ModelState.IsValid) return BadRequest(_responsehelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var model = JsonConvert.DeserializeObject<DowloadInsuranceProfileViewModel>(decryptedString.Item2);

            var response = _insuranceService.DowloadInsuranceProfileExcelData(model.SubStatus, model.ActiveStatus, model.Service);
            if (response.Status)
            {
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                string fileName = "HealthInsurance.xlsx";

                var content = response.Data as byte[];
                return File(content, contentType, fileName);
            }
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(response));

            return BadRequest(data);
        }
    }
}
