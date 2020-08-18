using HealthBanc.Domain.Models;
using HealthBanc.Response.AxaMansard;
using HealthBanc.Services.Insurance;
using HealthBanc.ViewModels.AxaMansard;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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

        public InsuranceController(InsuranceService insuranceService)
        {
            _insuranceService = insuranceService;
        }

        //var result = _insuranceService.AxaMansardGetToken(userName, password);
        ////var jsResult = JsonConvert.DeserializeObject<GetTokenResult>(result);
        //XmlSerializer serializer = new XmlSerializer(typeof(GetTokenResponse));
        //StringReader stringReader = new StringReader(result);
        //GetTokenResponse getToken = (GetTokenResponse)serializer.Deserialize(stringReader);
        //    return Ok(getToken);

        [HttpGet("[action]")]
        public IActionResult AxaMansardGetToken(string userName,string password)
        {           
            try
            {
                var result = _insuranceService.AxaMansardGetToken(userName, password);
                StringReader stringReader = new StringReader(result.Data);
                //var stringReader = @"<?xml version=""1.0"" encoding=""utf - 8""?><soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema""><soap:Body><getTokenResponse xmlns=""http://tempuri.org/""><getTokenResult><IsSuccessful>true</IsSuccessful><message>V2NXM01D2EgGUGzHgbbBTbxPAeFY/LokUwBUAEIAQQBOAEsAMAAxAA==</message><ReturnedObject xsi:type=""xsd:string"">STERLINGBANK</ReturnedObject><ReturnCode>00</ReturnCode></getTokenResult></getTokenResponse></soap:Body></soap:Envelope>";
                //StringReader stringReaders = new StringReader(stringReader);
                //XmlDocument doc = new XmlDocument();
                //doc.LoadXml(stringReader);
               // string json = Newtonsoft.Json.JsonConvert.SerializeXmlNode(doc);
                XmlSerializer serializer = new XmlSerializer(typeof(Envelope));
                Envelope getToken = (Envelope)serializer.Deserialize(stringReader);
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
            catch(Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpGet("[action]")]
        public IActionResult AxaMansardCreateUserProfile(UserProfileviewModel userProfile)
        {
            var result = _insuranceService.AxaMansardCreateUserProfile(userProfile);
            return Ok();
        }

        [HttpGet("[action]")]
        public IActionResult AxaMansardGetMedicalCondition()
        {
            return Ok();
        }

        [HttpGet("[action]")]
        public IActionResult AxaMansardGetTowns(string state)
        {
            return Ok();
        }

        [HttpGet("[action]")]
        public IActionResult AxaMansardGetStates()
        {
            return Ok();
        }

    }
}
