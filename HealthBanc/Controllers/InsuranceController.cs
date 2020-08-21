using AutoMapper;
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
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace HealthBanc.Controllers
{
    [Route("v1/api/[controller]")]
    [ApiController]
    public class InsuranceController : ControllerBase
    {
        private readonly InsuranceService _insuranceService;
        private readonly IMapper _mapper;

        public InsuranceController(InsuranceService insuranceService, IMapper mapper)
        {
            _insuranceService = insuranceService;
            _mapper = mapper;
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
                    getResponse.ReturnCode = xmlDoc2.GetElementsByTagName("ReturnCode").Item(0).InnerText;

                    return Ok(new ResponseMessage<AxaResponse> { Data = getResponse, Message = getResponse.Message, Status = true });

                }
                return BadRequest(new ResponseMessage { Message = "An error occurred while connecting to aza mansard. Time Out" });
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseMessage { Message = "An error occurred while connecting to aza mansard" });
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> AxaMansardCreateUserProfile([FromForm] UserProfileviewModel userProfile)
        {
            var tokenResult = _insuranceService.AxaMansardGetToken();
            if (tokenResult.Status == true)
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(tokenResult.Data);
                var token = xmlDoc.GetElementsByTagName("message").Item(0).InnerText;
                var base64Photo = await _insuranceService.GetBase64(userProfile.CustomerPhoto, userProfile.Surname);
                var base64Identity = await _insuranceService.GetBase64(userProfile.IdentityPhoto, userProfile.Surname);
                var profile = _mapper.Map<UserProfile>(userProfile);
                profile.IdentityPhoto = base64Identity; profile.CustomerPhoto = base64Photo;
                var result = _insuranceService.AxaMansardCreateUserProfile(profile, token);
                if (result.Status == true)
                {
                    XmlDocument xmlDoc2 = new XmlDocument();
                    xmlDoc2.LoadXml(result.Data);

                    var getResponse = new AxaResponse();
                    getResponse.IsSuccessful = xmlDoc2.GetElementsByTagName("IsSuccessful").Item(0).InnerText;
                    getResponse.Message = xmlDoc2.GetElementsByTagName("message").Item(0).InnerText;
                    getResponse.ReturnCode = xmlDoc2.GetElementsByTagName("ReturnCode").Item(0).InnerText;

                    return Ok(new ResponseMessage<AxaResponse> { Data = getResponse, Message = getResponse.Message, Status = true });
                }
                return BadRequest(new ResponseMessage { Message = "Connection Timeout. Error occurred while trying to coonecting to axa mansard" });
            }
            return BadRequest(new ResponseMessage { Message = "An error occurred while fetching token fro  axa mansard: Connection timeout", Status = false });
        }

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

                    var getResponse = new AxaResponse();
                    getResponse.Message = xmlDoc2.GetElementsByTagName("GetMedicalConditionsResult").Item(0).InnerText;
                    return Ok(new ResponseMessage { Data = getResponse, Message = "Medical condition was fetched successfully", Status = true });
                }
                return BadRequest(new ResponseMessage { Message = "Connection Timeout. Error occurred while trying coonecting to axa mansard" });
            }
            return BadRequest(new ResponseMessage { Message = "An error occurred while fetching token fro  axa mansard: Connection timeout", Status = false });
        }

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

                    var getResponse = new AxaResponse();
                    getResponse.Message = xmlDoc2.GetElementsByTagName("GetTownsResult").Item(0).InnerText;
                    return Ok(new ResponseMessage { Data = getResponse, Message = "Town was fetched successfully", Status = true });
                }
                return BadRequest(new ResponseMessage { Message = "An error occurred while fetching token fro  axa mansard: Connection timeout", Status = false });
            }
            return BadRequest(new ResponseMessage { Message = "And error occurred while trying to get token from axa mansard" });
        }

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

                    var getResponse = new AxaResponse();
                    getResponse.Message = xmlDoc2.GetElementsByTagName("GetStatesResult").Item(0).InnerText;
                    return Ok(new ResponseMessage { Data = getResponse, Message = "State was fetched successfully", Status = true });
                }
                return BadRequest(new ResponseMessage { Message = "An error occurred while fetching State from  axa mansard: Connection timeout", Status = false });
            }
            return BadRequest(new ResponseMessage { Message = "And error occurred while trying to get token from axa mansard" });
        }

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

                    var response = xmlDoc2.GetElementsByTagName("GetHealthPlansResult").Item(0).InnerText;
                    var responseResult = JsonConvert.DeserializeObject<List<AxaListResponse>>(response);
                    return Ok(new ResponseMessage<List<AxaListResponse>> { Data = responseResult, Message = "HealthPan was fetched successfully", Status = true });
                }
                return BadRequest(new ResponseMessage<List<AxaListResponse>> { Message = "An error occurred while fetching healthplan from  axa mansard: Connection timeout", Status = false });
            }
            return BadRequest(new ResponseMessage<List<AxaListResponse>> { Message = "And error occurred while trying to get token from axa mansard" });
        }

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

                    var response = xmlDoc2.GetElementsByTagName("GetHealthPlansResult").Item(0).InnerText;
                    var responseResult = JsonConvert.DeserializeObject<List<AxaListResponse>>(response);
                    return Ok(new ResponseMessage<List<AxaListResponse>> { Data = responseResult, Message = "Identification was fetched successfully", Status = true });
                }
                return BadRequest(new ResponseMessage<List<AxaListResponse>> { Message = "An error occurred while fetching identification from  axa mansard: Connection timeout", Status = false });
            }
            return BadRequest(new ResponseMessage<List<AxaListResponse>> { Message = "And error occurred while trying to get token from axa mansard" });
        }

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

                var response = xmlDoc2.GetElementsByTagName("GetHealthPlansResult").Item(0).InnerText;
                var responseResult = JsonConvert.DeserializeObject<List<AxaListResponse>>(response);
                return Ok(new ResponseMessage<List<AxaListResponse>> { Data = responseResult, Message = "Identification was fetched successfully", Status = true });
            }
            return BadRequest(new ResponseMessage<List<AxaListResponse>> { Message = "An error occurred while fetching identification from  axa mansard: Connection timeout", Status = false });            
        }
    }
}
