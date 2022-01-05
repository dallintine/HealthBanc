using Application.API_RequestModel.HealthInsured;
using Application.AuditAndReport.AuditLog;
using Application.DTO;
using Application.HealthInsured_AxaMansard_Service.Insurance;
using Application.Helpers;
using Application.Services;
using Application.Services.HealthInsured;
using Application.Services.HealthInsured_AxaMansard.Insurance;
using Application.ViewModels;
using Application.ViewModels.HealthInsured;
using AutoMapper;
using DataAccess;
using Domain.Models.Axa_Hygeia_Insurance;
using Hangfire;
using Infrastructure.ImageService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
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
        private readonly HMOIntegrationService _integrationService;
        private readonly IMapper _mapper;
        private readonly IOptions<ConnectionStrings> connectionString;
        private readonly AuditLogService _auditLogServices;
        private readonly ImageService _imageService;
        private readonly ILogger<UtilityController> _logger;

        private ConnectionStrings ConnectionStrings { get; }

        public UtilityController(InsuranceService insuranceService,TokenizationService tokenizationService, IBSIntegrationService iBSIntegrationService,UtilityService utilityService,
            IRepositoryWrapper repoWrapper,HMOIntegrationService integrationService,IMapper mapper, IOptions<ConnectionStrings> connectionString,
            AuditLogService auditLogServices,ImageService imageService,ILogger<UtilityController> logger)
        {
            _insuranceService = insuranceService;
            _tokenizationService = tokenizationService;
            _iBSIntegrationService = iBSIntegrationService;
            _utilityService = utilityService;
            _repoWrapper = repoWrapper;
            _integrationService = integrationService;
            _mapper = mapper;
            this.connectionString = connectionString;
            _auditLogServices = auditLogServices;
            _imageService = imageService;
            _logger = logger;
            ConnectionStrings = connectionString.Value;
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

        [Authorize(Roles = "Super-Administrator")]
        [HttpPost("[action]")]
        public IActionResult DeleteText(List<string> blob)
        {
            foreach(var item in blob)
            {
                _imageService.DeleteImage("logfolder", item);
            }
            return Ok();
        }

        [Authorize(Roles = "Super-Administrator")]
        [HttpGet("[action]")]
        public async Task<IActionResult> ListFiles(string blob, string prefix)
        {
             var files = await _imageService.ListFiles(blob,prefix);
            return Ok(files);
        }

        [Authorize(Roles = "Super-Administrator")]
        [HttpGet("[action]")]
        public async Task<IActionResult> Load()
        {
            var load = new EnrollmentModel();
            load.Email = "hassan@gmail.com";
            load.Gender = "2";
            for (int i = 0; i < 3; i++)
            {
                var auditViewModel = new AuditLogViewModel(1, null, null, "Created HealthInsured profile", "Created HealthInsured profile");
                BackgroundJob.Schedule(() => _auditLogServices.UserCreateAuditLog(auditViewModel, "1234", "test"),DateTime.Now.AddMinutes(5)) ;
                await _integrationService.AxamansardRegisterUser(load);
            }
            return Ok();  
        }

        [Authorize(Roles = "Super-Administrator")]
        [HttpGet("[action]")]
        public ActionResult HangfireExecute(string query)
        {
            string sqlConnectionString = ConnectionStrings.HangfireConnection;

            string script = query;

            SqlConnection connection = new SqlConnection(sqlConnectionString);

            Server server = new Server(new ServerConnection(connection));

            server.ConnectionContext.ExecuteNonQuery(script);

            return Ok();
        }

        [Authorize(Roles = "Super-Administrator")]
        [HttpGet("[action]")]
        public ActionResult DBExecute(string query)
        {
            string sqlConnectionString = ConnectionStrings.DefaultConnection;

            string script = query;

            SqlConnection connection = new SqlConnection(sqlConnectionString);

            Server server = new Server(new ServerConnection(connection));

            server.ConnectionContext.ExecuteNonQuery(script);

            return Ok();
        }

        [Authorize(Roles = "Super-Administrator")]
        [HttpGet("[action]")]
        public IActionResult CheckActiveAxamansard()
        {
            var users = _repoWrapper.InsuranceProfile.QueryAllInsuranceProfiles().Where(x => (x.InsuranceService.ToLower() == InsuranceProvider.Axamansard.ToString().ToLower()
             || x.InsuranceService == null) && x.SubscriptionStatus == true && x.AxamasardReferenceCode == null).ToList();

            return Ok(users);
        }

        [Authorize(Roles = "Super-Administrator")]
        [HttpGet("[action]")]
        public async Task<IActionResult> SendActiveAxamansard()
        {
            var users = _repoWrapper.InsuranceProfile.QueryAllInsuranceProfiles().Where(x => (x.InsuranceService.ToLower() == InsuranceProvider.Axamansard.ToString().ToLower()
             || x.InsuranceService == null) && x.SubscriptionStatus == true && x.AxamasardReferenceCode == null).ToList();

            foreach (var item in users)
            {
                var enrollmentModel = _mapper.Map<EnrollmentModel>(users);
                if(item.FamilyProfileId != null)
                {
                    enrollmentModel.Email = item.FamilyEmail;
                }
                var axaRegResponse = await _integrationService.EnrollUserToAxamansardOnOnboarding(item);
                var codeReference = axaRegResponse.Data as string;
                item.AxamasardReferenceCode = codeReference;
                _repoWrapper.InsuranceProfile.Update(item);
                await _repoWrapper.Save();
            }
            return Ok();
        }

        [Authorize(Roles = "Super-Administrator")]
        [HttpGet("[action]")]
        public async Task<IActionResult> SetActiveAxamansard(string enrollmentNumber, string reference)
        {
            var user = _repoWrapper.InsuranceProfile.QueryAllInsuranceProfiles().FirstOrDefault(x => x.TransId == enrollmentNumber && x.SubscriptionStatus == true 
            && (x.InsuranceService.ToLower() == InsuranceProvider.Axamansard.ToString().ToLower() || x.InsuranceService == null));

            if (user is null) return NotFound();
            user.AxamasardReferenceCode = reference;
            _repoWrapper.InsuranceProfile.Update(user);
            await _repoWrapper.Save();
            return Ok();
        }

        [Authorize(Roles = "Super-Administrator")]
        [HttpGet("[action]")]
        public async Task<IActionResult> CancelSubscription(string email, int insuranceProfileId)
        {
            var insuranceProfile = new List<InsuranceUserProfile>();
            if (email != null)
            {
                var queryProfiles = _repoWrapper.InsuranceProfile.QueryAllInsuranceProfiles();
                var profiles = queryProfiles.Where(x => x.Email == email).ToList();
                if(profiles != null) insuranceProfile = profiles;
            }
            else
            {
                var profile = await _repoWrapper.InsuranceProfile.GetByIdAsync(insuranceProfileId);
                if(profile != null) insuranceProfile.Add(profile);
            }
            foreach(var item in insuranceProfile)
            {
                var scheduledJobId = item.PendingJobId;

                BackgroundJob.Delete(scheduledJobId);
                if (item.PendingEmailJobId != null)
                {
                    BackgroundJob.Delete(item.PendingEmailJobId);
                }
                await _tokenizationService.ProcessUserActiveStatusCancellation(item.Id, null);               
            }
            return Ok(insuranceProfile);
        }

        [Authorize(Roles = "Super-Administrator")]
        [HttpGet("[action]")]
        public async Task<IActionResult> SchedulePaymentIndivdualInsuranceProfile()
        {
            var users = _repoWrapper.InsuranceProfile.QueryAllInsuranceProfiles().Where(x => x.SubscriptionStatus == true &&
            x.EndActiveStatusDate < DateTime.Now && x.ActiveStatus == true).ToList();

            foreach (var item in users)
            {
                item.PendingJobId = _tokenizationService.ProcessScheduledPayment(item);
                _repoWrapper.InsuranceProfile.Update(item);
                await _repoWrapper.Save();
            }

            await _repoWrapper.Save();
            return Ok();
        }

        [Authorize(Roles = "Super-Administrator")]
        [HttpGet("[action]")]
        public async Task<IActionResult> SchedulePaymentCoroporateInsuranceProfile()
        {
            var companyProfiles = _repoWrapper.CompanyProfile.QueryAllCompanyProfiles().Where(x => x.TokenizationCompleted == true &&
            x.NextPaymentDate < DateTime.Now &&  x.ProfileCompleted == true).ToList();

            foreach (var item in companyProfiles)
            {
                item.PendingJobId = _tokenizationService.ProcessScheduledPayment(item);
                _repoWrapper.CompanyProfile.Update(item);
                await _repoWrapper.Save();
            }
            return Ok();
        }
    }
}
