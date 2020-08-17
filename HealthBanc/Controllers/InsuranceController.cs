using HealthBanc.Services.Insurance;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml;

namespace HealthBanc.Controllers
{
    public class InsuranceController : ControllerBase
    {
        private readonly InsuranceService _insuranceService;

        public InsuranceController(InsuranceService insuranceService)
        {
            _insuranceService = insuranceService;
        }

        [HttpPost("[action]")]
        public IActionResult AxaMansardGetToken(string userName,string password)
        {
            var result = _insuranceService.AxaMansardGetToken(userName, password);
            return Ok(result);
        }

        //public async Task<IActionResult> AxaMansardCreateUserProfile()
        //{
           
        //}
    }
}
