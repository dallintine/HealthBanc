using Application.API_RequestModel.HealthInsured;
using Application.API_RequestModel.Wallet;
using Application.API_ResponseModel.Wallet;
using Application.AuditAndReport.AuditLog;
using Application.DTO;
using Application.HealthInsured_AxaMansard_Service.Insurance;
using Application.Helpers;
using Application.Interfaces;
using Application.Services;
using Application.Services.HealthInsured;
using Application.Services.HealthInsured_AxaMansard.Insurance;
using Application.Services.Wallet;
using Application.ViewModels;
using Application.ViewModels.HealthInsured;
using Application.ViewModels.HealthInsured.Wallet;
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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
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
        private readonly ISMSService _smsService;
        private readonly AuditLogService _auditLogServices;
        private readonly ImageService _imageService;
        private readonly ILogger<UtilityController> _logger;
        private readonly IWalletEncryptionsAndDecryption _encryptionsAndDecryption;
        private readonly WalletConnect _walletConnect;

        private ConnectionStrings ConnectionStrings { get; }

        public UtilityController(InsuranceService insuranceService,TokenizationService tokenizationService, IBSIntegrationService iBSIntegrationService,UtilityService utilityService,
            IRepositoryWrapper repoWrapper,HMOIntegrationService integrationService,IMapper mapper, IOptions<ConnectionStrings> connectionString, ISMSService smsService,
            AuditLogService auditLogServices,ImageService imageService,ILogger<UtilityController> logger, IWalletEncryptionsAndDecryption encryptionsAndDecryption,
            WalletConnect walletConnect)
        {
            _insuranceService = insuranceService;
            _tokenizationService = tokenizationService;
            _iBSIntegrationService = iBSIntegrationService;
            _utilityService = utilityService;
            _repoWrapper = repoWrapper;
            _integrationService = integrationService;
            _mapper = mapper;
            this.connectionString = connectionString;
            _smsService = smsService;
            _auditLogServices = auditLogServices;
            _imageService = imageService;
            _logger = logger;
            _encryptionsAndDecryption = encryptionsAndDecryption;
            _walletConnect = walletConnect;
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
        public async Task<IActionResult> SendActiveAxamansard_NoReference()
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
        public async Task<IActionResult> SendActiveAxamansard_IgnoreReference()
        {
            var users = _repoWrapper.InsuranceProfile.QueryAllInsuranceProfiles().Where(x => (x.InsuranceService.ToLower() == InsuranceProvider.Axamansard.ToString().ToLower()
             || x.InsuranceService == null) && x.SubscriptionStatus == true).ToList();

            foreach (var item in users)
            {
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
        public async Task<IActionResult> SendActiveHygeia_NoTransId()
        {
            var users = _repoWrapper.InsuranceProfile.QueryAllInsuranceProfiles().Where(x => (x.InsuranceService.ToLower() == InsuranceProvider.Hygeia.ToString().ToLower())
            && x.SubscriptionStatus == true && x.TransId == null).ToList();

            foreach (var item in users)
            {
                var hygeiaRegResponse = await _integrationService.EnrollUserToHygeiaOnOnboarding(item);
                var codeReference = hygeiaRegResponse.Data as string;
                item.TransId = codeReference;
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
        public async Task<IActionResult> CancelInactiveUsersSub(string provider)
        {
            var users = _repoWrapper.InsuranceProfile.QueryAllInsuranceProfiles().Where(x => (x.InsuranceService.ToLower() == provider.ToLower())
            && x.SubscriptionStatus == false && x.TransId != null).ToList();

            foreach (var item in users)
            {
                var scheduledJobId = item.PendingJobId;

                BackgroundJob.Delete(scheduledJobId);
                if (item.PendingEmailJobId != null)
                {
                    BackgroundJob.Delete(item.PendingEmailJobId);
                }
                await _tokenizationService.ProcessUserActiveStatusCancellation(item.Id, null);
            }
            return Ok();
        }


        [Authorize(Roles = "Super-Administrator")]
        [HttpGet("[action]")]
        public async Task<IActionResult> SchedulePaymentIndivdualInsuranceProfile(int count)
        {
            var users = _repoWrapper.InsuranceProfile.QueryAllInsuranceProfiles().Where(x => x.SubscriptionStatus == true &&
            x.EndActiveStatusDate < DateTime.Now && x.ActiveStatus == true && x.CompanyProfileId == null).Take(count).ToList();

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
        public async Task<IActionResult> SchedulePaymentCoroporateInsuranceProfile(int count)
        {
            var companyProfiles = _repoWrapper.CompanyProfile.QueryAllCompanyProfiles().Where(x => x.TokenizationCompleted == true &&
            x.NextPaymentDate < DateTime.Now &&  x.ProfileCompleted == true).Take(count).ToList();

            foreach (var item in companyProfiles)
            {
                item.PendingJobId = _tokenizationService.ProcessScheduledPayment(item);
                _repoWrapper.CompanyProfile.Update(item);
                await _repoWrapper.Save();
            }
            return Ok();
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetProfielDetailsByMailOrId(string email, int id)
        {
            if(email != null)
            {
                var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByEmail(email);
                if (insuranceProfile != null)
                {
                    return Ok(insuranceProfile);
                }
                var familyProfile = await _repoWrapper.FamilyProfile.GetByEmail(email);
                if (familyProfile != null)
                {
                    return Ok(familyProfile);
                }
                var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyProfileByEmail(email);
                if (companyProfile != null)
                {
                    return Ok(companyProfile);
                }
                return Ok();
            }
            else
            {
                var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByIdAsync(id);
                if (insuranceProfile != null)
                {
                    return Ok(insuranceProfile);
                }
                var familyProfile = await _repoWrapper.FamilyProfile.GetExtendedFamilyDetailsById(id);
                if (familyProfile != null)
                {
                    return Ok(familyProfile);
                }
                var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyInsuranceProfiles(id);
                if (companyProfile != null)
                {
                    return Ok(companyProfile);
                }
                return Ok();
            }
            
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetProfielDetailsByUserId(int userId)
        {
            var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByUserIdAsync(userId);
            if (insuranceProfile != null)
            {
                return Ok(insuranceProfile);
            }
            var familyProfile = await _repoWrapper.FamilyProfile.GetByUserId(userId);
            if (familyProfile != null)
            {
                return Ok(familyProfile);
            }
            var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyInsuranceUserProfilesByUserId(userId);
            if (companyProfile != null)
            {
                return Ok(companyProfile);
            }
            return Ok();
        }

        [Authorize(Roles = "Super-Administrator")]
        [HttpGet("[action]")]
        public async Task<IActionResult> SchedulePaymentForIndividualInsurance(int insuranceProfileId)
        {
            var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByIdAsync(insuranceProfileId);
            if (insuranceProfile is null) return NotFound("Insurance Profile Not Found");
            insuranceProfile.PendingJobId = _tokenizationService.ProcessScheduledPayment(insuranceProfile);
            _repoWrapper.InsuranceProfile.Update(insuranceProfile);
            await _repoWrapper.InsuranceProfile.Save();
            return Ok("scheduled successfully");
        }

        [Authorize(Roles = "Super-Administrator")]
        [HttpGet("[action]")]
        public async Task<IActionResult> SchedulePaymentForCompanyInsurance(int companyId)
        {
            var companyProfile = await _repoWrapper.CompanyProfile.GetCompanyBeneficiaryReviewUsersByCompanyId(companyId);
            if (companyProfile is null) return NotFound("Company Profile Not Found");
            companyProfile.PendingJobId = _tokenizationService.ProcessScheduledPayment(companyProfile);
            _repoWrapper.CompanyProfile.Update(companyProfile);
            await _repoWrapper.CompanyProfile.Save();
            return Ok("scheduled successfully");
        }

        [HttpGet("[action]")]
        public ActionResult GetAllActiveUsers(bool active, bool sub,int skip,int take)
        {
            var users = _repoWrapper.InsuranceProfile.QueryAllInsuranceProfiles().Where(x => x.SubscriptionStatus == sub &&
            x.ActiveStatus == active).Skip(skip).Take(take).ToList();
            return Ok(users);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> OnboardHygeia(RegistrationModel registrationModel)
        {
            var resp = await _integrationService.HygeiaRegisterUser(registrationModel);
            return Ok(resp);

        }

        [HttpGet("[action]")]
        public async Task<IActionResult> SMSTest()
        {
            var smsresponse = await _smsService.SendSmsAsync("07034770338", "test test");
            return Ok(smsresponse);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> WalletDetails(string mobileNumber)
        {
            var walletData = new GetWalletDetails(mobileNumber);
            _logger.LogInformation($"Processing Wallet Details For [Mobile :{walletData.Mobile}]\n");
            var encryptData = _encryptionsAndDecryption.Encrypt(JsonConvert.SerializeObject(walletData));
            var encryptedModel = new EncryptedModel(encryptData);
            var validateWalletResponse = await _walletConnect.WalletDetails(encryptedModel);
            var decryptedResponse = _encryptionsAndDecryption.Decrypt(validateWalletResponse);
            _logger.LogInformation($"Validate Wallet decrypted response : {decryptedResponse}");
            var response = JsonConvert.DeserializeObject<ApiResponse<WalletValidationResponse>>(decryptedResponse);
            return Ok(new ResponseMessage {Message = decryptedResponse ,  Data = response });
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> CreateWallet(CreateWallet walletModel)
        {
            var createWalletData = new CreateWallet
            {
                Firstname = walletModel.Firstname,
                Lastname = walletModel.Lastname,
                Mobile = walletModel.Mobile,
                DOB = walletModel.DOB,
                Gender = walletModel.Gender,
                ChannelId = walletModel.ChannelId,
                ProductId = walletModel.ProductId
            };
            var payload = JsonConvert.SerializeObject(createWalletData);
            _logger.LogInformation($"Create Wallet Payload [Payload : {payload}]\n");
            var encryptCreatWalletData = _encryptionsAndDecryption.Encrypt(payload);
            var encryptedCreateWalletModel = new EncryptedModel(encryptCreatWalletData);
            var createWalletResponse = await _walletConnect.CreateWallet(encryptedCreateWalletModel);
            var decryptedCreateWalletResponse = _encryptionsAndDecryption.Decrypt(createWalletResponse);
            _logger.LogInformation($"Create Wallet decrypted response : {decryptedCreateWalletResponse}");
            var walletResponse = JsonConvert.DeserializeObject<CreateWalletResponse>(decryptedCreateWalletResponse);
            return Ok(new ResponseMessage { Message = decryptedCreateWalletResponse, Data = walletResponse });
        }
    }
}
