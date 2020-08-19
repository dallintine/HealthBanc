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

        //var result = _insuranceService.AxaMansardGetToken(userName, password);
        ////var jsResult = JsonConvert.DeserializeObject<GetTokenResult>(result);
        //XmlSerializer serializer = new XmlSerializer(typeof(GetTokenResponse));
        //StringReader stringReader = new StringReader(result);
        //GetTokenResponse getToken = (GetTokenResponse)serializer.Deserialize(stringReader);
        //    return Ok(getToken);

        [HttpGet("[action]")]
        public IActionResult AxaMansardGetToken()
        {           
            try
            {                
                var result = _insuranceService.AxaMansardGetToken();
                if(result.Status != false)
                {
                     StringReader stringReader = new StringReader(result.Data);
                //var stringReader = @"<?xml version=""1.0"" encoding=""utf - 8""?><soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema""><soap:Body><getTokenResponse xmlns=""http://tempuri.org/""><getTokenResult><IsSuccessful>true</IsSuccessful><message>V2NXM01D2EgGUGzHgbbBTbxPAeFY/LokUwBUAEIAQQBOAEsAMAAxAA==</message><ReturnedObject xsi:type=""xsd:string"">STERLINGBANK</ReturnedObject><ReturnCode>00</ReturnCode></getTokenResult></getTokenResponse></soap:Body></soap:Envelope>";
                //StringReader stringReaders = new StringReader(stringReader);
                //XmlDocument doc = new XmlDocument();
                //doc.LoadXml(stringReader);
               // string json = Newtonsoft.Json.JsonConvert.SerializeXmlNode(doc);
                XmlSerializer serializer = new XmlSerializer(typeof(EnvelopeTokenResponse));
                EnvelopeTokenResponse getToken = (EnvelopeTokenResponse)serializer.Deserialize(stringReader);
                return Ok(getToken);

                //var result = _insuranceService.AxaMansardGetToken(userName, password);
                //if(result.Status == true)
                //{
                //    XmlDocument xmlDoc = new XmlDocument();
                //    xmlDoc.LoadXml(stringReader);
                //    var getToken = new GetTokenResult();
                //    getToken.ReturnCode = xmlDoc.GetElementsByTagName("ReturnCode").Item(0).InnerText;
                //    getToken.IsSuccessful = xmlDoc.GetElementsByTagName("IsSuccessful").Item(0).InnerText;
                //    getToken.Message = xmlDoc.GetElementsByTagName("message").Item(0).InnerText;
                //    return Ok(getToken);
                //}
                //return BadRequest(result);
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
                    return Ok(new ResponseMessage { Data = result.Data, Message = "Profile was created successfully" ,Status=true});
                }
                return BadRequest(new ResponseMessage { Message="Connection Timeout. Error occurred while trying to coonecting to axa mansard"});
            }
            return BadRequest(new ResponseMessage {Message= "An error occurred while fetching token fro  axa mansard: Connection timeout",Status=false });           
        }

        [HttpGet("[action]")]
        public IActionResult AxaMansardGetMedicalCondition()
        {
            var tokenResult = _insuranceService.AxaMansardGetToken();
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(tokenResult.Data);
            var token = xmlDoc.GetElementsByTagName("message").Item(0).InnerText;
            var result = _insuranceService.AxaMansardGetMedicalCondition(token);
            return Ok(result);
        }

        [HttpGet("[action]")]
        public IActionResult AxaMansardGetTowns(string state)
        {
            var tokenResult = _insuranceService.AxaMansardGetToken();
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(tokenResult.Data);
            var token = xmlDoc.GetElementsByTagName("message").Item(0).InnerText;
            var result = _insuranceService.AxaMansardGetTowns(state,token);
            return Ok(result);
        }

        [HttpGet("[action]")]
        public IActionResult AxaMansardGetStates()
        {
            //var tokenResult = _insuranceService.AxaMansardGetToken();
            //XmlDocument xmlDoc = new XmlDocument();
            //xmlDoc.LoadXml(tokenResult.Data);
            //var token = xmlDoc.GetElementsByTagName("message").Item(0).InnerText;
            //var result = _insuranceService.AxaMansardGetStates(token);
            var result = @"<?xml version=""1.0"" encoding=""utf - 8""?><soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema""><soap:Body><GetStatesResponse xmlns=""http://tempuri.org/""><GetStatesResult>[{""Code"":""6"",""Text"":""Abia""},{""Code"":""7"",""Text"":""Adamawa""}]</GetStatesResult></GetStatesResponse></soap:Body></soap:Envelope>";
          
            StringReader stringReader = new StringReader(result);
            XmlSerializer serializer = new XmlSerializer(typeof(EnvelopeGetStates));
            //XmlSerializer serializer = new XmlSerializer(typeof(List<StateArray>), new XmlRootAttribute("EnvelopeGetStates"));
            EnvelopeGetStates getToken = (EnvelopeGetStates)serializer.Deserialize(stringReader);
            return Ok(getToken);
        }

    }
}
