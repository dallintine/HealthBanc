using HealthBanc.Domain.Models;
using HealthBanc.Response.AxaMansard;
using HealthBanc.Services.Insurance;
using HealthBanc.ViewModels.AxaMansard;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
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

        [HttpGet("[action]")]
        public IActionResult AxaMansardGetToken(string userName,string password)
        {
            var result = _insuranceService.AxaMansardGetToken(userName, password);
            //var jsResult = JsonConvert.DeserializeObject<GetTokenResult>(result);
            XmlSerializer serializer = new XmlSerializer(typeof(GetTokenResponse));
            StringReader stringReader = new StringReader(result);
            GetTokenResponse getToken = (GetTokenResponse)serializer.Deserialize(stringReader);
            return Ok(getToken);
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
