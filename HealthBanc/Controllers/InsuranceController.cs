using AutoMapper;
using HealthBanc.Domain.Models;
using HealthBanc.Request.AxaMansard;
using HealthBanc.Response;
using HealthBanc.Response.AxaMansard;
using HealthBanc.Services.Insurance;
using HealthBanc.ViewModels.AxaMansard;
using Microsoft.AspNetCore.Mvc;
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

        public InsuranceController(InsuranceService insuranceService,IMapper mapper)
        {
            _insuranceService = insuranceService;
            _mapper = mapper;
        }

        [HttpGet("[action]")]
        public IActionResult AxaMansardGetToken()
        {           
            try
            {                
                var result = _insuranceService.AxaMansardGetToken();
                if(result.Status != false)
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
            catch(Exception ex)
            {
                return BadRequest(new ResponseMessage { Message = "An error occurred while connecting to aza mansard" });
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> AxaMansardCreateUserProfile([FromForm]UserProfileviewModel userProfile)
        {
            var tokenResult = _insuranceService.AxaMansardGetToken();
            if(tokenResult.Status == true)
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(tokenResult.Data);
                var token = xmlDoc.GetElementsByTagName("message").Item(0).InnerText;
                var base64Photo = await _insuranceService.GetBase64(userProfile.CustomerPhoto, userProfile.Surname);
                var base64Identity = await _insuranceService.GetBase64(userProfile.IdentityPhoto, userProfile.Surname);
                var profile = _mapper.Map<UserProfile>(userProfile);
                profile.IdentityPhoto = base64Identity; profile.CustomerPhoto = base64Photo;
                var result = _insuranceService.AxaMansardCreateUserProfile(profile, token);
                if(result.Status == true)
                {
                    XmlDocument xmlDoc2 = new XmlDocument();
                    xmlDoc2.LoadXml(result.Data);

                    var getResponse = new AxaResponse();
                    getResponse.IsSuccessful = xmlDoc2.GetElementsByTagName("IsSuccessful").Item(0).InnerText;
                    getResponse.Message = xmlDoc2.GetElementsByTagName("message").Item(0).InnerText;
                    getResponse.ReturnCode = xmlDoc2.GetElementsByTagName("ReturnCode").Item(0).InnerText;

                    return Ok(new ResponseMessage<AxaResponse> { Data = getResponse, Message = getResponse.Message , Status=true});
                }
                return BadRequest(new ResponseMessage { Message="Connection Timeout. Error occurred while trying to coonecting to axa mansard"});
            }
            return BadRequest(new ResponseMessage {Message= "An error occurred while fetching token fro  axa mansard: Connection timeout",Status=false });           
        }

        [HttpGet("[action]")]
        public IActionResult AxaMansardGetMedicalCondition()
        {
            var tokenResult = _insuranceService.AxaMansardGetToken();
            if(tokenResult.Status == true)
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(tokenResult.Data);
                var token = xmlDoc.GetElementsByTagName("message").Item(0).InnerText;
                var result = _insuranceService.AxaMansardGetMedicalCondition(token);
                if(result.Status == true)
                {
                    XmlDocument xmlDoc2 = new XmlDocument();
                    xmlDoc2.LoadXml(result.Data);

                    var getResponse = new AxaResponse();
                    getResponse.IsSuccessful = xmlDoc2.GetElementsByTagName("IsSuccessful").Item(0).InnerText;
                    getResponse.Message = xmlDoc2.GetElementsByTagName("message").Item(0).InnerText;
                    getResponse.ReturnCode = xmlDoc2.GetElementsByTagName("ReturnCode").Item(0).InnerText;
                    return Ok(new ResponseMessage { Data= getResponse, Message="Medical condition was fetched successfully",Status=true});
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
                if(result.Status == true)
                {
                    XmlDocument xmlDoc2 = new XmlDocument();
                    xmlDoc2.LoadXml(result.Data);

                    var getResponse = new AxaResponse();
                    getResponse.IsSuccessful = xmlDoc2.GetElementsByTagName("IsSuccessful").Item(0).InnerText;
                    getResponse.Message = xmlDoc2.GetElementsByTagName("message").Item(0).InnerText;
                    getResponse.ReturnCode = xmlDoc2.GetElementsByTagName("ReturnCode").Item(0).InnerText;
                    return Ok(new ResponseMessage { Data = getResponse, Message = "Medical condition was fetched successfully", Status = true });
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
                    getResponse.IsSuccessful = xmlDoc2.GetElementsByTagName("IsSuccessful").Item(0).InnerText;
                    getResponse.Message = xmlDoc2.GetElementsByTagName("message").Item(0).InnerText;
                    getResponse.ReturnCode = xmlDoc2.GetElementsByTagName("ReturnCode").Item(0).InnerText;
                    return Ok(new ResponseMessage { Data = getResponse, Message = "Medical condition was fetched successfully", Status = true });
                }
                return BadRequest(new ResponseMessage { Message = "An error occurred while fetching token from  axa mansard: Connection timeout", Status = false });
            }
            return BadRequest(new ResponseMessage { Message = "And error occurred while trying to get token from axa mansard" });
        }
    }
}
