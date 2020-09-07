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
        private readonly ApplicationDbContext _dbContext;
        private readonly IApplicationUserRepository _userRepository;
        private readonly IAxaMansardCompletionRepository _completionRepository;

        public InsuranceController(InsuranceService insuranceService, IMapper mapper,IAxaMansardUserProfileRepository axaMansard,ILogger<InsuranceController> logger,
            ApplicationDbContext dbContext, IApplicationUserRepository userRepository, IAxaMansardCompletionRepository completionRepository)
        {
            _insuranceService = insuranceService;
            _mapper = mapper;
            _axaMansard = axaMansard;
            _logger = logger;
            _dbContext = dbContext;
            _userRepository = userRepository;
            _completionRepository = completionRepository;
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
        public IActionResult AxaMansardGetToken()
        {
            try
            {               
                var result = _insuranceService.AxaMansardGetToken();
                if (result.Status != false)
                {
                    XmlDocument xmlDoc2 = new XmlDocument();
                    xmlDoc2.LoadXml(result.Data);

                    var getResponse = new AxaResponse();
                    getResponse.IsSuccessful = xmlDoc2.GetElementsByTagName("IsSuccessful").Item(0).InnerText;
                    getResponse.Message = xmlDoc2.GetElementsByTagName("message").Item(0).InnerText;

                    return Ok(new ResponseMessage<AxaResponse> { Data = getResponse, Message = getResponse.Message, Status = true });

                }
                return BadRequest(new ResponseMessage { Message = "An error occurred while connecting to aza mansard. Time Out" });
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseMessage { Message = "An error occurred while connecting to aza mansard" });
            }
        }

        [Authorize]
        [HttpPost("[action]")]
        public async Task<IActionResult> AxaMansardCreateUserProfile([FromForm] UserProfileviewModel userProfile)
        {
            try
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

                var tokenResult = _insuranceService.AxaMansardGetToken();
            if (tokenResult.Status == true)
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(tokenResult.Data);
                var profile = _mapper.Map<UserProfile>(userProfile);
                profile.Surname = User.FindFirst("LastName")?.Value; profile.Othernames = User.FindFirst("FirstName")?.Value; profile.Email = User.FindFirstValue(ClaimTypes.Email);
                profile.PhoneNumber = User.FindFirst("PhoneNumber")?.Value;

                var base64Photo = await _insuranceService.GetBase64(userProfile.CustomerPhoto, profile.Surname);
                var base64Identity = await _insuranceService.GetBase64(userProfile.IdentityPhoto, profile.Surname);
                profile.IdentityPhoto = base64Identity; profile.CustomerPhoto = base64Photo;

                var token = xmlDoc.GetElementsByTagName("message").Item(0).InnerText;
                var returnCode = xmlDoc.GetElementsByTagName("ReturnCode").Item(0).InnerText;
                if(returnCode == "00")
                {
                    //var premiumResult = _insuranceService.AxaMansardPremiumPlan(profile.PlanCode, token);

                    if (/*premiumResult.Status ==*/ true)
                    {
                        //XmlDocument xmlDoc2 = new XmlDocument();
                        //xmlDoc2.LoadXml(premiumResult.Data);
                        //var premiumResponse = xmlDoc2.GetElementsByTagName("GetHealthPremiumResult").Item(0).InnerText;

                        profile.Premium = Decimal.Parse("5600");

                        //profile.Premium = Decimal.Parse(premiumResponse);
                        var result = _insuranceService.AxaMansardCreateUserProfile(profile, token);
                        if (result.Status == true)
                        {
                            XmlDocument xmlDoc3 = new XmlDocument();
                            xmlDoc3.LoadXml(result.Data);

                            var getResponse = new AxaResponse();
                            getResponse.IsSuccessful = xmlDoc3.GetElementsByTagName("IsSuccessful").Item(0).InnerText ?? "";
                            getResponse.Message = xmlDoc3.GetElementsByTagName("message").Item(0).InnerText ?? "";

                            var axaInsuranceUser = _mapper.Map<AxaMansardUserProfile>(profile);
                            axaInsuranceUser.SuperAdminId = Id;
                            _axaMansard.Create(axaInsuranceUser);
                            await _axaMansard.Save();

                            var completionProfile = new AxaMansardCompletionProfile(Id, true, false);
                            _completionRepository.Create(completionProfile);
                            await _completionRepository.Save();

                            var newServiceString = user.ServiceUsed + "HealthInsured,";
                            user.ServiceUsed = newServiceString;
                            _userRepository.Update(user);
                            await _userRepository.Save();

                            return Ok(new ResponseMessage<AxaResponse> { Data = getResponse, Message = getResponse.Message, Status = true });
                        }
                        var getResponse2 = new AxaResponse();
                        getResponse2.Message = token;
                        getResponse2.IsSuccessful = xmlDoc.GetElementsByTagName("IsSuccessful").Item(0).InnerText;
                        return BadRequest(new ResponseMessage { Message = getResponse2.Message,Data=getResponse2 });
                    }
                    //return BadRequest(new ResponseMessage { Message = "An error occurred while fetching HealthPremium from  axa mansard: Connection timeout", Status = false });
                }
                return BadRequest(new ResponseMessage { Message = "An error occurred while fetching token fro  axa mansard: Connection timeout", Status = false });
            }
            return BadRequest(new ResponseMessage { Message = "Token could not be fetched,please try again later: Connection timeout", Status = false });
            }
            catch(Exception ex)
            {
                _logger.LogCritical("An error occure when trying to create profile "+ex);
                return BadRequest();
            }            
        }

        [Authorize]
        [HttpGet("[action]")]
        public IActionResult AxaMansardGetMedicalCondition()
        {
            var tokenResult = _insuranceService.AxaMansardGetToken();
            if (tokenResult.Status == true)
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(tokenResult.Data);
                var token = xmlDoc.GetElementsByTagName("message").Item(0).InnerText;
                var result = _insuranceService.AxaMansardGetMedicalCondition(token);
                if (result.Status == true)
                {
                    XmlDocument xmlDoc2 = new XmlDocument();
                    xmlDoc2.LoadXml(result.Data);

                    var response = xmlDoc2.GetElementsByTagName("GetMedicalConditionsResult").Item(0).InnerText;
                    var responseResult = JsonConvert.DeserializeObject<List<AxaListResponse>>(response);
                    return Ok(new ResponseMessage<List<AxaListResponse>> { Data = responseResult, Message = "Medical condition was fetched successfully", Status = true });
                }
                return BadRequest(new ResponseMessage { Message = "Connection Timeout. Error occurred while trying coonecting to axa mansard" });
            }
            return BadRequest(new ResponseMessage { Message = "An error occurred while fetching token fro  axa mansard: Connection timeout", Status = false });
        }

        [Authorize]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<AxaListResponseRoot>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage<AxaListResponseRoot>))]
        [HttpGet("[action]")]
        public IActionResult AxaMansardGetTowns(string state)
        {
            var tokenResult = _insuranceService.AxaMansardGetToken();
            if (tokenResult.Status == true)
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(tokenResult.Data);
                var token = xmlDoc.GetElementsByTagName("message").Item(0).InnerText;
                var result = _insuranceService.AxaMansardGetTowns(state, token);
                if (result.Status == true)
                {
                    XmlDocument xmlDoc2 = new XmlDocument();
                    xmlDoc2.LoadXml(result.Data);

                    var response = xmlDoc2.GetElementsByTagName("GetTownsResult").Item(0).InnerText;
                    var responseResult = JsonConvert.DeserializeObject<List<AxaListResponse>>(response);
                    return Ok(new ResponseMessage<List<AxaListResponse>> { Data = responseResult, Message = "Town was fetched successfully", Status = true });
                }
                return BadRequest(new ResponseMessage { Message = "An error occurred while fetching token fro  axa mansard: Connection timeout", Status = false });
            }
            return BadRequest(new ResponseMessage { Message = "And error occurred while trying to get token from axa mansard" });
        }

        [Authorize]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<AxaListResponseRoot>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage<AxaListResponseRoot>))]
        [HttpGet("[action]")]
        public IActionResult AxaMansardGetStates()
        {
            var tokenResult = _insuranceService.AxaMansardGetToken();
            if (tokenResult.Status == true)
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(tokenResult.Data);
                var token = xmlDoc.GetElementsByTagName("message").Item(0).InnerText;
                var result = _insuranceService.AxaMansardGetStates(token);
                if (result.Status == true)
                {

                    XmlDocument xmlDoc2 = new XmlDocument();
                    xmlDoc2.LoadXml(result.Data);

                    var response = xmlDoc2.GetElementsByTagName("GetStatesResult").Item(0).InnerText;
                    var responseResult = JsonConvert.DeserializeObject<List<AxaListResponse>>(response);
                    return Ok(new ResponseMessage<List<AxaListResponse>> { Data = responseResult, Message = "State was fetched successfully", Status = true });
                }
                return BadRequest(new ResponseMessage { Message = "An error occurred while fetching State from  axa mansard: Connection timeout", Status = false });
            }
            return BadRequest(new ResponseMessage { Message = "And error occurred while trying to get token from axa mansard" });
        }

        [Authorize]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<AxaListResponseRoot>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage<AxaListResponseRoot>))]
        [HttpGet("[action]")]
        public IActionResult AxaMansardGetHealthPlans()
        {
            var tokenResult = _insuranceService.AxaMansardGetToken();
            if (tokenResult.Status == true)
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(tokenResult.Data);
                var token = xmlDoc.GetElementsByTagName("message").Item(0).InnerText;
                var result = _insuranceService.AxaMansardGetHealthPlans(token);
                if (result.Status == true)
                {
                    XmlDocument xmlDoc2 = new XmlDocument();
                    xmlDoc2.LoadXml(result.Data);

                    var response = xmlDoc2.GetElementsByTagName("GetHealthPlansResult").Item(0).InnerText;
                    var responseResult = JsonConvert.DeserializeObject<List<AxaListResponse>>(response);
                    return Ok(new ResponseMessage<List<AxaListResponse>> { Data = responseResult, Message = "HealthPan was fetched successfully", Status = true });
                }
                return BadRequest(new ResponseMessage<List<AxaListResponse>> { Message = "An error occurred while fetching healthplan from  axa mansard: Connection timeout", Status = false });
            }
            return BadRequest(new ResponseMessage<List<AxaListResponse>> { Message = "And error occurred while trying to get token from axa mansard" });
        }

        [Authorize]
        [HttpGet("[action]")]
        public IActionResult AxaMansardGetHealthPremium(string planCode)
        {
            var tokenResult = _insuranceService.AxaMansardGetToken();
            if (tokenResult.Status == true)
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(tokenResult.Data);
                var token = xmlDoc.GetElementsByTagName("message").Item(0).InnerText;
                var result = _insuranceService.AxaMansardPremiumPlan(planCode, token);
                if (result.Status == true)
                {

                    XmlDocument xmlDoc2 = new XmlDocument();
                    xmlDoc2.LoadXml(result.Data);

                    var getResponse = new AxaResponse();
                    getResponse.Message = xmlDoc2.GetElementsByTagName("GetHealthPremiumResult").Item(0).InnerText;
                    return Ok(new ResponseMessage { Data = getResponse, Message = "HealthPremium was fetched successfully", Status = true });
                }
                return BadRequest(new ResponseMessage { Message = "An error occurred while fetching HealthPremium from  axa mansard: Connection timeout", Status = false });
            }
            return BadRequest(new ResponseMessage { Message = "And error occurred while trying to get token from axa mansard" });
        }

        [Authorize]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<AxaListResponseRoot>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage<AxaListResponseRoot>))]
        [HttpGet("[action]")]
        public IActionResult AxaMansardGetReligion()
        {
            var tokenResult = _insuranceService.AxaMansardGetToken();
            if (tokenResult.Status == true)
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(tokenResult.Data);
                var token = xmlDoc.GetElementsByTagName("message").Item(0).InnerText;
                var result = _insuranceService.AxaMansardGetReligion(token);
                if (result.Status == true)
                {
                    XmlDocument xmlDoc2 = new XmlDocument();
                    xmlDoc2.LoadXml(result.Data);

                    var response = xmlDoc2.GetElementsByTagName("GetReligionResult").Item(0).InnerText;
                    var responseResult = JsonConvert.DeserializeObject<List<AxaListResponse>>(response);
                    return Ok(new ResponseMessage<List<AxaListResponse>> { Data = responseResult, Message = "HealthPan was fetched successfully", Status = true });
                }
                return BadRequest(new ResponseMessage<List<AxaListResponse>> { Message = "An error occurred while fetching healthplan from  axa mansard: Connection timeout", Status = false });
            }
            return BadRequest(new ResponseMessage<List<AxaListResponse>> { Message = "And error occurred while trying to get token from axa mansard" });
        }

        [Authorize]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<AxaListResponseRoot>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage<AxaListResponseRoot>))]
        [HttpGet("[action]")]
        public IActionResult AxaMansardGetIdentificationType()
        {
            var tokenResult = _insuranceService.AxaMansardGetToken();
            if (tokenResult.Status == true)
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(tokenResult.Data);
                var token = xmlDoc.GetElementsByTagName("message").Item(0).InnerText;
                var result = _insuranceService.AxaMansardGetIdentification(token);
                if (result.Status == true)
                {
                    XmlDocument xmlDoc2 = new XmlDocument();
                    xmlDoc2.LoadXml(result.Data);

                    var response = xmlDoc2.GetElementsByTagName("GetIdentificationTypesResult").Item(0).InnerText;
                    var responseResult = JsonConvert.DeserializeObject<List<AxaListResponse>>(response);
                    return Ok(new ResponseMessage<List<AxaListResponse>> { Data = responseResult, Message = "Identification was fetched successfully", Status = true });
                }
                return BadRequest(new ResponseMessage<List<AxaListResponse>> { Message = "An error occurred while fetching identification from  axa mansard: Connection timeout", Status = false });
            }
            return BadRequest(new ResponseMessage<List<AxaListResponse>> { Message = "And error occurred while trying to get token from axa mansard" });
        }

        [Authorize]
        [ProducesResponseType(200, Type = typeof(ResponseMessage<AxaListResponseRoot>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage<AxaListResponseRoot>))]
        [HttpGet("[action]")]
        public IActionResult AxaMansardGetBanks()
        {            
            var result = _insuranceService.AxaMansardGetBanks();
            if (result.Status == true)
            {
                XmlDocument xmlDoc2 = new XmlDocument();
                xmlDoc2.LoadXml(result.Data);

                var response = xmlDoc2.GetElementsByTagName("GetBanksResult").Item(0).InnerText;
                var responseResult = JsonConvert.DeserializeObject<List<AxaListResponse>>(response);
                return Ok(new ResponseMessage<List<AxaListResponse>> { Data = responseResult, Message = "Identification was fetched successfully", Status = true });
            }
            return BadRequest(new ResponseMessage<List<AxaListResponse>> { Message = "An error occurred while fetching identification from  axa mansard: Connection timeout", Status = false });            
        }

        [Authorize]
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

        [HttpGet("[action]")]
        public async Task<IActionResult> Delete()
        {
            var test = await _dbContext.TestUserProfiles.ToListAsync();
            _dbContext.TestUserProfiles.RemoveRange(test);
            await _dbContext.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            var result = await _insuranceService.CBNList(file);
            await _dbContext.TestUserProfiles.AddRangeAsync(result);
            await _dbContext.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetExcelFromDB()
        {
            try
            {
                //step1: create array to holder header labels
                string[] col_names = new string[]{
                "S/N",
                "TRANSID",
                "FIRSTNAME",
                "SURNAME",
                "OTHER NAMES",
                "MARITAL STATUS",
                "ADDRESS",
                "DATE OF BIRTH",
                "NEXT OF KIN",
                "PHONE NUMBER",
                "PLAN",
                "SEX"
            };

                var userList = await _dbContext.TestUserProfiles.ToListAsync();
                //step2: create result byte array
                byte[] result;

                //ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                //step3: create a new package using memory safe structure
                using (var package = new ExcelPackage())
                {
                    //step4: create a new worksheet
                    var worksheet = package.Workbook.Worksheets.Add("AXA MANSARD USERS");

                    //step5: fill in header row
                    //worksheet.Cells[row,col].  {Style, Value}
                    for (int i = 0; i < col_names.Length; i++)
                    {
                        worksheet.Cells[1, i + 1].Style.Font.Size = 14;  //font
                        worksheet.Cells[1, i + 1].Value = col_names[i];  //value
                        worksheet.Cells[1, i + 1].Style.Font.Bold = true; //bold  
                    }

                    int row = 2;
                    int count = 1;
                    //step6: loop through query result and fill in cells
                    foreach (var item in userList)
                    {
                        for (int col = 1; col <= 2; col++)
                        {
                            worksheet.Cells[row, col].Style.Font.Size = 12;
                        }
                        //set row,column data
                        worksheet.Cells[row, 1].Value = count;
                        worksheet.Cells[row, 2].Value = item.TransId;
                        worksheet.Cells[row, 3].Value = item.FirstName;
                        worksheet.Cells[row, 4].Value = item.SurnName;
                        worksheet.Cells[row, 5].Value = item.OtherName;
                        worksheet.Cells[row, 6].Value = item.MaritalStatus;
                        worksheet.Cells[row, 7].Value = item.Address;
                        worksheet.Cells[row, 8].Value = item.DateOfBirth;
                        worksheet.Cells[row, 9].Value = item.NextofKin;
                        worksheet.Cells[row, 10].Value = item.PhoneNumber;
                        worksheet.Cells[row, 11].Value = item.Plan;
                        worksheet.Cells[row, 12].Value = item.Sex;

                        if (row == 170)
                        {
                            row += 2;
                        }

                        row++;
                        count++;
                    }
                    //step7: auto fit columns
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    //step8: convert the package as byte array
                    result = package.GetAsByteArray();
                }//end using
                //step9: return byte array as a file   
                return File(result, "application/vnd.ms-excel", "Users.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogCritical("An error occurred while trying to generate user excel file: " + ex);
                return BadRequest();
            }
        }

        [ProducesResponseType(200, Type = typeof(ResponseMessage<HealthInsuredProfileStateDTO>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage<HealthInsuredProfileStateDTO>))]
        [HttpGet("[action]")]
        [Authorize]
        public async Task<IActionResult> GetProfileCompletion()
        {
            try
            {
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int Id = int.Parse(userId);

                var profile = await _completionRepository.GetCompletionStateBySuperAdminId(Id);
                if (profile == null)
                {
                    var profileState = new HealthInsuredProfileStateDTO(false, false);
                    return Ok(new ResponseMessage<HealthInsuredProfileStateDTO> { Data = profileState, Status = true, Message = "Profile completion state was fetched successfully"});
                }
                var profileSate2 = new HealthInsuredProfileStateDTO(profile.ProfileCompleted, profile.TokenizationCompleted);
                return Ok(new ResponseMessage<HealthInsuredProfileStateDTO> { Data = profileSate2, Status = true, Message = "Profile completion state was fetched successfully" });
            }
            catch(Exception ex)
            {
                _logger.LogCritical("An error occurred while trying to get user profile completion: " + ex);
                return BadRequest(new ResponseMessage<HealthInsuredProfileStateDTO>{ Message = "An error occurred while trying to get user profile information.Please try again later" });
            }            
        }
    }
}
