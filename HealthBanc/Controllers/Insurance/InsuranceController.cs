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
        private readonly AuditLogService _auditLogServices;
        private readonly IHttpContextAccessor _accessor;
        public string IpAddress;
        public StringValues agent;

        public InsuranceController(InsuranceService insuranceService, IMapper mapper,IInsuranceProfileRepository insuranceProfileRepository,ILogger<InsuranceController> logger,
            IApplicationUserRepository userRepository, IInsuranceCompletionProfileRepository completionRepository, AuditLogService auditLogServices,
            IHttpContextAccessor accessor)
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
        }

        [HttpGet("[action]")]
        public List<string> GetState()
        {
            var stateList = new List<string>()

            { "Abia","Abuja","Adamawa","AkwaIbom","Anambra","Bauchi","Bayelsa","Benue","Borno","Cross River","Delta","Ebonyi","Edo","Ekiti",
             "Enugu","Gombe","Imo","Jigawa","Kaduna","Kano","Katsina","Kebbi","Kogi","Kwara","Lagos","Nasarawa","Niger",
             "Ogun","Ondo","Osun","Oyo","Plateau","Rivers","Sokoto","Taraba","Yobe","Zamfara"
            };
            return stateList;
        }

        [EnableCors("Cors")]
        [HttpPost("[action]")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> AxaMansardCreateUserProfile([FromForm] UserProfileviewModel userProfile)
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


        //[Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<List<CityListDTO>>))]
        [HttpGet("[action]")]
        public IActionResult AxaMansardGetTowns(string state)
        {
            var townList = _insuranceService.GetTowns(state);
            return Ok(townList);
        }


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


        [HttpGet("[action]")]
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<AxaMansardUserDTO>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage<AxaMansardUserDTO>))]
        public async Task<IActionResult> GetAxaMansardProfile()
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);
            var profile = await _insuranceProfileRepository.GetByUserIdAsync(Id);
            if(profile != null)
            {
                var profileDTO = _mapper.Map<AxaMansardUserDTO>(profile);
                return Ok(new ResponseMessage<AxaMansardUserDTO> {Data= profileDTO, Message="User profile was fetched successfully",Status=true });
            }
            return BadRequest(new ResponseMessage<AxaMansardUserDTO> { Message = "Profile was not found" });
        }


        [HttpGet("[action]")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<HealthInsuredProfileStateDTO>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage<HealthInsuredProfileStateDTO>))]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetProfileCompletion()
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            var profile = await _completionRepository.GetCompletionStateByUserId(Id);
            if (profile == null)
            {
                var notFoundProfileState = new HealthInsuredProfileStateDTO(false, false,null);
                return Ok(new ResponseMessage<HealthInsuredProfileStateDTO> { Data = notFoundProfileState, Status = true, Message = "Profile completion state was fetched successfully" });
            }
            var profileState = new HealthInsuredProfileStateDTO(profile.ProfileCompleted, profile.TokenizationCompleted,profile.ServiceUsed);
            return Ok(new ResponseMessage<HealthInsuredProfileStateDTO> { Data = profileState, Status = true, Message = "Profile completion state was fetched successfully" });
        }


        [HttpPost("[action]")]
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


        [HttpGet("[action]")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> AxaMansardGetHealthProvider(string state, string city, string healthPlan)
        {
            var healthProvider = await _insuranceService.AxaMansardGetHealthProvider(state, city, healthPlan);
            return Ok(healthProvider);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            var result = await _insuranceService.AxaHospitalList(file);
            return Ok();
        }
    }
}



































































//[HttpGet("[action]")]
//public async Task<IActionResult> Test()
//{
//    var axamansardProfile = new AxaMansardUserProfile()
//    {
//        TransId = "123456712345",
//        SubscriptionStatus = false,
//        TownOfResidence = "Lag",
//        StateOfResidence = "Lag",
//        IdentityPhoto = "Lag",
//        CustomerPhoto = "Lag",
//        Premium = Decimal.Parse("1000"),
//        PlanCode = "Lag",
//        AlternateHospital = "Lag",
//        CPEmail = "Lag",
//        CPCity = "Lag",
//        CPAddress = "Lag",
//        CPPhone = "Lag",
//        CareProviderName = "Lag",
//        Identification = "ID",
//        MaritalStatus = "Single",
//        Occupation = "Lag",
//        ContactAddress = "Lag",
//        Email = "Lag@gmail.com",
//        PhoneNumber = "07034770338",
//        DateOfBirth = DateTime.Now,
//        Othernames = "Lag",
//        Surname = "Lag",
//        Gender = "Male",
//    };
//    var listDTO = _mapper.Map<InactiveUsersDTO>(axamansardProfile);
//    await _liveExcelList.WriteAsync(listDTO);
//    return Ok();
//}


//[HttpGet("[action]")]
//public IActionResult AxaMansardGetToken()
//{
//    try
//    {               
//        var result = _insuranceService.AxaMansardGetToken();
//        if (result.Status != false)
//        {
//            XmlDocument xmlDoc2 = new XmlDocument();
//            xmlDoc2.LoadXml(result.Data);

//            var getResponse = new AxaResponse();
//            getResponse.IsSuccessful = xmlDoc2.GetElementsByTagName("IsSuccessful").Item(0).InnerText;
//            getResponse.Message = xmlDoc2.GetElementsByTagName("message").Item(0).InnerText;

//            return Ok(new ResponseMessage<AxaResponse> { Data = getResponse, Message = getResponse.Message, Status = true });

//        }
//        return BadRequest(new ResponseMessage { Message = "An error occurred while connecting to aza mansard. Time Out" });
//    }
//    catch (Exception ex)
//    {
//        return BadRequest(new ResponseMessage { Message = "An error occurred while connecting to aza mansard" });
//    }
//}
