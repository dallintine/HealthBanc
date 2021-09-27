using Application.DTO;
using Application.HealthInsured_AxaMansard_Service.Insurance;
using Application.Services;
using Application.Services.HealthInsured_AxaMansard.Insurance;
using Application.ViewModels.HealthInsured;
using DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Controllers
{
    [Route("v1/api/[controller]")]
    [ApiController]
    public class UtilityController : ControllerBase
    {
        private readonly InsuranceService _insuranceService;
        private readonly TokenizationService _tokenizationService;
        private readonly IBSIntegrationService _iBSIntegrationService;
        private readonly UtilityService _utilityService;
        private readonly IRepositoryWrapper _repoWrapper;

        public UtilityController(InsuranceService insuranceService,TokenizationService tokenizationService, IBSIntegrationService iBSIntegrationService,UtilityService utilityService,
            IRepositoryWrapper repoWrapper)
        {
            _insuranceService = insuranceService;
            _tokenizationService = tokenizationService;
            _iBSIntegrationService = iBSIntegrationService;
            _utilityService = utilityService;
            _repoWrapper = repoWrapper;
        }

        /// <summary>
        /// Download insurance profile data
        /// </summary>
        /// <param name="subStatus"></param>
        /// <param name="activeStatus"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        [Authorize(Roles = "Super-Administrator")]
        [ProducesResponseType(200, Type = typeof(File))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        public IActionResult DowloadInsuranceProfileExcelData(bool? subStatus, bool? activeStatus, string service)
        {
            var response = _insuranceService.DowloadInsuranceProfileExcelData(subStatus, activeStatus, service);
            if (response.Status)
            {
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                string fileName = "HealthInsurance.xlsx";

                var content = response.Data as byte[];
                return File(content, contentType, fileName);
            }
            return BadRequest(response);
        }

        /// <summary>
        /// Upload Axamansard Hospital List
        /// </summary>
        /// <param name="file"></param>
        /// <param name="passcode"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "Super-Administrator")]
        public async Task<IActionResult> UploadAxamansardHospitalListToDb(IFormFile file, string passcode)
        {
            if (passcode == "docUpload1963.")
            {
                var checkRole = User.IsInRole("Super-Administrator");
                if (checkRole)
                {
                    var result = await _utilityService.UploadAxaHospitalListFromExcel(file);
                    if (result.Status)
                    {
                        return Ok(result);
                    }
                    return BadRequest();
                }
                return BadRequest(new ResponseMessage { Message = "You dont have permission to access this resource. Request for permission." });
            }
            return BadRequest(new ResponseMessage { Message = "Wrong passcode" });
        }

        /// <summary>
        /// Upload Hygeia hospital List
        /// </summary>
        /// <param name="file"></param>
        /// <param name="passcode"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "Super-Administrator")]
        public async Task<IActionResult> UploadHygeiaHospitalListToDb(IFormFile file, string passcode)
        {
            if (passcode == "docUpload1963.")
            {
                var checkRole = User.IsInRole("Super-Administrator");
                if (checkRole)
                {
                    var result = await _utilityService.UploadHygeiaHospitalListFromExcel(file);
                    if (result.Status)
                    {
                        return Ok(result);
                    }
                    return BadRequest();
                }
                return BadRequest(new ResponseMessage { Message = "You dont have permission to access this resource. Request for permission." });
            }
            return BadRequest(new ResponseMessage { Message = "Wrong passcode" });
        }

        /// <summary>
        /// Admin Update individual insurance profile
        /// </summary>
        /// <param name="updateProfileViewModel"></param>
        /// <param name="email"></param> 
        /// <returns></returns>
        [HttpPost("[action]")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "Super-Administrator")]
        public async Task<IActionResult> AdminUpdateProfileAsync(string email, UpdateProfileViewModel updateProfileViewModel)
        {
            if (ModelState.IsValid)
            {
                var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByEmail(email);

                var updateResponse = await _insuranceService.UpdateProfileAsync(updateProfileViewModel, insuranceProfile.UserId.Value);
                if (updateResponse.Status)
                {
                    return Ok(updateResponse);
                }
                return BadRequest(updateResponse);
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

        /// <summary>
        /// Fix card errors
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        [Authorize(Roles = "Super-Administrator")]
        [ProducesResponseType(200, Type = typeof(OkObjectResult))]
        public async Task<IActionResult> FixCardsError(string email)
        {
            await _utilityService.FixCardsError(email);
            return Ok();
        }

        /// <summary>
        /// Get HMO invoice details
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "Super-Administrator")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        public IActionResult GetHMOInvoiceDetails()
        {
            var response = _utilityService.GetHMOInvoiceDetailsForMonthEnd();
            return Ok(response);
        }

        [Authorize(Roles = "Super-Administrator")]
        [HttpGet("[action]")]
        public async Task<IActionResult> FixPaymentError(string reference, string amount)
        {
            await _utilityService.FitPaymentError(reference, amount);
            return Ok();
        }

        /// <summary>
        /// Perform sterling intra bank transfer
        /// </summary>
        /// <param name="toAccount"></param>
        /// <param name="fromAccount"></param>
        /// <param name="amount"></param>
        /// <param name="des"></param>
        /// <returns></returns>
        [Authorize(Roles = "Super-Administrator")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        public async Task<IActionResult> SendMoney(string toAccount, string fromAccount, string amount, string des)
        {
            var res = await _iBSIntegrationService.SterlingBankIntraBank(decimal.Parse(amount), toAccount, fromAccount, des, "NG0020032");
            return Ok(res);
        }

        [Authorize(Roles = "Super-Administrator")]
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        public async Task<IActionResult> ProcessFailedHMOPayment(int Id)
        {
            await _utilityService.ProcessFailedHMOPayment(Id);
            return Ok();
        }
    }
}
