using AutoMapper;
using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Domain.Models;
using HealthBanc.Request.AxaMansard;
using HealthBanc.Response;
using HealthBanc.Response.AxaMansard;
using HealthBanc.Services.Insurance;
using HealthBanc.ViewModels.AxaMansard;
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
using HealthBanc.Data;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using HealthBanc.DTO.ApplicationUserDTOs;
using AxaMansardSoap;
using Microsoft.Extensions.Configuration;
using HealthBanc.Helpers;
using Microsoft.Extensions.Options;
using HealthBanc.Services.InsuredCancelLiveSheet;
using HealthBanc.DTO;
using System.Globalization;
using HealthBanc.ViewModels;
using Hangfire;
using HealthBanc.Services.AuditAndReport.AuditLog;

namespace HealthBanc.Controllers
{
    [Route("v1/api/[controller]")]
    [ApiController]
    public class InsuranceController : ControllerBase
    {
        private readonly InsuranceService _insuranceService;
        private readonly IMapper _mapper;
        private readonly IAxaMansardUserProfileRepository _axaMansard;
        private readonly ILogger<InsuranceController> _logger;
        private readonly IApplicationUserRepository _userRepository;
        private readonly IAxaMansardCompletionRepository _completionRepository;
        private readonly AuditLogService _auditLogServices;
        private readonly IAxaMansardSoap _axaMansardSoap;
        private readonly LiveExcelList _liveExcelList;

        private Towns Options { get; }

        public InsuranceController(InsuranceService insuranceService, IMapper mapper,IAxaMansardUserProfileRepository axaMansard,ILogger<InsuranceController> logger,
            IApplicationUserRepository userRepository, IAxaMansardCompletionRepository completionRepository, AuditLogService auditLogServices,
            IAxaMansardSoap axaMansardSoap, IOptions<Towns> optionAccessor, LiveExcelList liveExcelList)
        {
            Options = optionAccessor.Value;
            _insuranceService = insuranceService;
            _mapper = mapper;
            _axaMansard = axaMansard;
            _logger = logger;
            _userRepository = userRepository;
            _completionRepository = completionRepository;
            _auditLogServices = auditLogServices;
            _axaMansardSoap = axaMansardSoap;
            _liveExcelList = liveExcelList;
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

        [HttpGet("[action]")]
        public async Task<IActionResult> AxaMansardGetToken()
        {
            var result = await _axaMansardSoap.GetTokenRequest();
            return Ok(result.Body.getTokenResult);
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("[action]")]
        public async Task<IActionResult> AxaMansardCreateUserProfile([FromForm] UserProfileviewModel userProfile)
        {
            if (ModelState.IsValid)
            {
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int Id = int.Parse(userId);
                var user = await _userRepository.FindByIdAsync(Id);

                var checkIfUserHasBeenProfiled = await _axaMansard.GetByAdminIdAsync(Id);
                if (checkIfUserHasBeenProfiled != null) return BadRequest(new ResponseMessage { Message = "User has a profile already" });


                var supportedTypes = new[] { ".JPG", ".JPE", ".PNG", ".JPEG" };
                var customerPhoto = System.IO.Path.GetExtension(userProfile.CustomerPhoto.FileName).ToUpperInvariant();
                var identityPhoto = System.IO.Path.GetExtension(userProfile.IdentityPhoto.FileName).ToUpperInvariant();

                if (!supportedTypes.Contains(customerPhoto) || !supportedTypes.Contains(identityPhoto))
                {
                    return BadRequest(new ResponseMessage { Message = "FIle extension is invalid - Only Upload PNG/JPEG/JPG/JPE" });
                }

                var customerPhotorSize = userProfile.CustomerPhoto.Length;
                var identityPhotoSize = userProfile.IdentityPhoto.Length;
                if ((customerPhotorSize / 1048576) > 4 || (identityPhotoSize / 1048576) > 4)
                {
                    return BadRequest(new ResponseMessage { Message = "Image size is too large - Size should be less than four Megabyte" });
                }

                var tokenResult = await _axaMansardSoap.GetTokenRequest();
                if (tokenResult.Body.getTokenResult.IsSuccessful == true)
                {
                    var token = tokenResult.Body.getTokenResult.message;

                    var profile = _mapper.Map<iHealth>(userProfile);
                    profile.Surname = User.FindFirst("LastName")?.Value; profile.Othernames = User.FindFirst("FirstName")?.Value; profile.Email = User.FindFirstValue(ClaimTypes.Email);
                    profile.PhoneNumber = User.FindFirst("PhoneNumber")?.Value;

                    profile.CareProviderName = userProfile.CareProviderName.Split(":")[0]; profile.CPAddress = userProfile.CareProviderName.Split(":")[1];
                    profile.AlternateHospital = userProfile.AlternateHospital.Split(":")[0];

                    var base64Photo = await _insuranceService.GetBase64(userProfile.CustomerPhoto, profile.Surname);
                    var base64Identity = await _insuranceService.GetBase64(userProfile.IdentityPhoto, profile.Surname);
                    profile.IdentityPhoto = base64Identity; profile.CustomerPhoto = base64Photo;
                    profile.TransId = _insuranceService.GetUniqueCode(12);                      

                    if (profile.PlanCode == "7")
                    {
                        profile.Premium = Decimal.Parse("1020");
                    }
                    else if (profile.PlanCode == "8")
                    {
                        profile.Premium = Decimal.Parse("2050");
                    }

                    //var result = await _axaMansardSoap.SaveHealth(profile, token);
                    if (/*result.Body.SaveHealthResult.IsSuccessful ==*/ true )
                    {
                        //var getResponse = new AxaResponse();
                        //getResponse.IsSuccessful = result.Body.SaveHealthResult.IsSuccessful.ToString();
                        //getResponse.Message = result.Body.SaveHealthResult.message;

                        var getResponse = new AxaResponse();
                        getResponse.IsSuccessful = "True";
                        getResponse.Message = "Profile was created successfully";

                        var axaInsuranceUser = _mapper.Map<AxaMansardUserProfile>(profile);
                        axaInsuranceUser.UserId = Id;
                        axaInsuranceUser.AlternateHospitalAddress = profile.AlternateHospital = userProfile.AlternateHospital.Split(":")[1];

                        var completionProfile = new AxaMansardCompletionProfile(Id, true, false);

                        if (!user.ServiceUsed.Contains("HealthInsured,"))
                        {
                            var newServiceString = user.ServiceUsed + "HealthInsured,";
                            user.ServiceUsed = newServiceString;
                            _userRepository.Update(user);
                        }
                        await _userRepository.Save();


                        var auditViewModel = new AuditLogViewModel(Id, null, null, "Created HealthInsured profile", "Created HealthInsured profile");
                        BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel));

                        return Ok(new ResponseMessage<AxaResponse> { Data = getResponse, Message ="Profile was created successfully", Status = true });
                    }
                    //var getResponse2 = new AxaResponse();
                    //getResponse2.Message = result.Body.SaveHealthResult.message;
                    //getResponse2.IsSuccessful = result.Body.SaveHealthResult.IsSuccessful.ToString();
                    //return BadRequest(new ResponseMessage { Message = getResponse2.Message, Data = getResponse2 });
                }
                return BadRequest(new ResponseMessage { Message = tokenResult.Body.getTokenResult.message, Status = false });
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


        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<AxaListResponseRoot>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage<AxaListResponseRoot>))]
        [HttpGet("[action]")]
        public IActionResult AxaMansardGetTowns(string state)
        {
            var towns = Options.town;
            if (towns != null)
            {
                var town = towns.Where(x => x.State == state).ToList();
                return Ok(new ResponseMessage { Data = town, Message = "Towns was fetched successfully", Status = true });
            }
            return BadRequest(new ResponseMessage { Message = "Towns could not  be fetched,please try again later", Status = false });
        }

        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<AxaListResponseRoot>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage<AxaListResponseRoot>))]
        [HttpGet("[action]")]
        public async Task<IActionResult> AxaMansardGetHealthPlans()
        {
            var tokenResult = await _axaMansardSoap.GetTokenRequest();
            if (tokenResult.Body.getTokenResult.IsSuccessful == true)
            {
                var result =await _axaMansardSoap.GetHealthPlansAsync(tokenResult.Body.getTokenResult.message);
                if (result.Body.GetHealthPlansResult != null)
                {
                    var response = result.Body.GetHealthPlansResult;
                    var responseResult = JsonConvert.DeserializeObject<List<AxaListResponse>>(response);
                    return Ok(new ResponseMessage<List<AxaListResponse>> { Data = responseResult, Message = "HealthPan was fetched successfully", Status = true });
                }
                return BadRequest(new ResponseMessage<List<AxaListResponse>> { Message = "Could not fetch healthplan from  axa mansard: Connection timeout", Status = false });
            }
            return BadRequest(new ResponseMessage<List<AxaListResponse>> { Message = tokenResult.Body.getTokenResult.message });
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("[action]")]
        public async Task<IActionResult> AxaMansardGetHealthPremium(string planCode)
        {
          
            var tokenResult = await _axaMansardSoap.GetTokenRequest();
            if (tokenResult.Body.getTokenResult.IsSuccessful == true)
            {
                var result =await _axaMansardSoap.GetHealthPremiumAsync(tokenResult.Body.getTokenResult.message, planCode);
                if (result.Body.GetHealthPremiumResult > 0)
                {
                    var getResponse = new AxaResponse();
                    getResponse.Message = result.Body.GetHealthPremiumResult.ToString();
                    return Ok(new ResponseMessage { Data = getResponse, Message = "HealthPremium was fetched successfully", Status = true });
                }
                return BadRequest(new ResponseMessage { Message = "Could not fetch HealthPremium from  axa mansard", Status = false });
            }
            return BadRequest(new ResponseMessage { Message = tokenResult.Body.getTokenResult.message });
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("[action]")]
        public async Task<IActionResult> AxaMansardGetHealthProvider(string state,string city,string healthPlan)
        {
            
            var tokenResult = await _axaMansardSoap.GetTokenRequest();
            var tokenBody = tokenResult.Body.getTokenResult;
            if (tokenBody.IsSuccessful == true)
            {
                var result = await _axaMansardSoap.GetHealthProvider(healthPlan, state, tokenBody.message);
                if (result.Body.GetHealthProvidersResult.Count > 0)
                {
                    var healthProvider = result.Body.GetHealthProvidersResult.Where(x => x.City.Contains(city)).ToList();
                    return Ok(new ResponseMessage { Data = healthProvider, Message = "HealthPlan was fetched successfully", Status = true });
                }
                return BadRequest(new ResponseMessage { Message = "No healthProvider tied to specified State,City and Plan", Status = false });
            }
            return BadRequest(new ResponseMessage { Message = tokenBody.message });
        }

        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<AxaMansardUserDTO>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage<AxaMansardUserDTO>))]
        [HttpGet("[action]")]
        public async Task<IActionResult> GetAxaMansardProfile()
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);
            var profile = await _axaMansard.GetByAdminIdAsync(Id);
            if(profile != null)
            {
                var profileDTO = _mapper.Map<AxaMansardUserDTO>(profile);
                profileDTO.EnrolleeNumber = _insuranceService.GetUniqueCode(7);
                return Ok(new ResponseMessage<AxaMansardUserDTO> {Data= profileDTO, Message="user profile was fetched successfully",Status=true });
            }
            return BadRequest(new ResponseMessage<AxaMansardUserDTO> { Message = "Profile was not found" });
        }
        

        [ProducesResponseType(200, Type = typeof(ResponseMessage<HealthInsuredProfileStateDTO>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage<HealthInsuredProfileStateDTO>))]
        [HttpGet("[action]")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetProfileCompletion()
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int Id = int.Parse(userId);

            var profile = await _completionRepository.GetCompletionStateBySuperAdminId(Id);
            if (profile == null)
            {
                var profileState = new HealthInsuredProfileStateDTO(false, false);
                return Ok(new ResponseMessage<HealthInsuredProfileStateDTO> { Data = profileState, Status = true, Message = "Profile completion state was fetched successfully" });
            }
            var profileSate2 = new HealthInsuredProfileStateDTO(profile.ProfileCompleted, profile.TokenizationCompleted);
            return Ok(new ResponseMessage<HealthInsuredProfileStateDTO> { Data = profileSate2, Status = true, Message = "Profile completion state was fetched successfully" });
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("[action]")]
        public async Task<IActionResult> UpdateProfileAsync(UpdateProfileViewModel updateProfileViewModel)
        {
            if (ModelState.IsValid)
            {
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int Id = int.Parse(userId);
                var checkIfUserHasBeenProfiled = await _axaMansard.GetByAdminIdAsync(Id);
                if (checkIfUserHasBeenProfiled == null) return BadRequest(new ResponseMessage { Message = "User does not have a profile" });

                var updatedProfile = _mapper.Map<UpdateProfileViewModel, AxaMansardUserProfile>(updateProfileViewModel, checkIfUserHasBeenProfiled);
                if (updatedProfile != null)
                {
                    _axaMansard.Update(updatedProfile);
                    await _axaMansard.Save();

                    var auditViewModel = new AuditLogViewModel(Id, null, null, "Updated HealthInsured Profile", "Updated HealthInsured profile");
                    BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel));
                    return Ok(new ResponseMessage { Status = true, Message = "Profile was updated successfully" });
                }
                return BadRequest(new ResponseMessage { Status = false, Message = "This on us...Error occurred while updating profile" });

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
