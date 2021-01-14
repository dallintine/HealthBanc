using Application.API_RequestModel.HealthInsured_AxaMansard;
using Application.API_ResponseModel.HealthInsured_AxaMansard;
using Application.DTO;
using Application.HealthInsured_AxaMansard_Service.AuditAndReport.AuditLog;
using Application.Helpers;
using Application.ViewModels;
using Application.ViewModels.AxaMansard;
using AutoMapper;
using DataAccess.General.Interfaces;
using DataAccess.HealthInsured_AxaMansard.Interfaces;
using Domain.Models;
using Domain.Models.AxaMansard_Insurance;
using Hangfire;
using HealthBanc.DTO.HealthInsured_AxaMansard;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Application.HealthInsured_AxaMansard_Service.Insurance
{
    public class InsuranceService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IApplicationUserRepository _userRepository;
        private readonly IAxaMansardUserProfileRepository _axaMansard;
        private readonly IMapper _mapper;
        private readonly IAxaMansardCompletionRepository _completionRepository;
        private readonly AuditLogService _auditLogServices;
        private readonly ExcelPackage _excelPackage;
        private readonly IAxaMansardHospitalListRepository _hospitalListRepository;
        private readonly ILogger<InsuranceService> _logger;

        private AxaMansardConfiguration Options { get; }

        public InsuranceService(IHttpClientFactory httpClientFactory, IOptions<AxaMansardConfiguration>  axaAccessor,IApplicationUserRepository userRepository
            , IAxaMansardUserProfileRepository axaMansard,IMapper mapper,IAxaMansardCompletionRepository completionRepository, AuditLogService auditLogServices,
            ExcelPackage excelPackage, IAxaMansardHospitalListRepository hospitalListRepository,ILogger<InsuranceService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _userRepository = userRepository;
            _axaMansard = axaMansard;
            _mapper = mapper;
            _completionRepository = completionRepository;
            _auditLogServices = auditLogServices;
            _excelPackage = excelPackage;
            _hospitalListRepository = hospitalListRepository;
            _logger = logger;
            Options = axaAccessor.Value;
        }        

        public async Task<ResponseMessage> AxaMansardOnboarding(UserProfileviewModel userProfile, int userId,string ipAddress,string device)
        {
            var user = await _userRepository.FindByIdAsync(userId);

            var checkIfUserHasBeenProfiled = await _axaMansard.GetByUserIdAsync(userId);
            if (checkIfUserHasBeenProfiled != null) return new ResponseMessage { Message = "User has a profile already" };


            var supportedTypes = new[] { ".JPG", ".JPE", ".PNG", ".JPEG" };
            var customerPhoto = System.IO.Path.GetExtension(userProfile.CustomerPhoto.FileName).ToUpperInvariant();
            var identityPhoto = System.IO.Path.GetExtension(userProfile.IdentityPhoto.FileName).ToUpperInvariant();

            if (!supportedTypes.Contains(customerPhoto) || !supportedTypes.Contains(identityPhoto))
            {
                return new ResponseMessage { Message = "FIle extension is invalid - Only Upload PNG/JPEG/JPG/JPE" };
            }

            var customerPhotorSize = userProfile.CustomerPhoto.Length;
            var identityPhotoSize = userProfile.IdentityPhoto.Length;
            if ((customerPhotorSize / 1048576) > 4 || (identityPhotoSize / 1048576) > 4)
            {
                return new ResponseMessage { Message = "Image size is too large - Size should be less than four Megabyte" };
            }

            CreateAxamansardUserProfile(userProfile, user);                

            var auditViewModel = new AuditLogViewModel(userId, null, null, "Created HealthInsured profile", "Created HealthInsured profile");
            BackgroundJob.Enqueue(() => _auditLogServices.UserCreateAuditLog(auditViewModel, ipAddress, device));

            return new ResponseMessage { Data = new AxaResponse() { IsSuccessful = "True", Message = "Profile was created successfully" },
                Message = "Profile was created successfully", Status = true };
        }

        private async void CreateAxamansardUserProfile(UserProfileviewModel userProfile,ApplicationUser user)
        {
            var profile = _mapper.Map<AxaMansardUserProfile>(userProfile);
            profile.UserId = user.Id;

            profile.CareProviderName = userProfile.CareProviderName.Split(":")[0]; profile.CPAddress = userProfile.CareProviderName.Split(":")[1];
            profile.TransId = GetUniqueCode(12);
            profile.CPCity = userProfile.CareProviderName.Split(":").Length == 3 ? userProfile.CareProviderName.Split(":")[2] : "";

            var updatedProfile = _mapper.Map(user, profile);

            _axaMansard.Create(updatedProfile);

            var completionProfile = new AxaMansardCompletionProfile(user.Id, true, false);
            _completionRepository.Create(completionProfile);

            var newServiceString = user.ServiceUsed + "HealthInsured,";
            user.ServiceUsed = newServiceString;
            _userRepository.Update(user);
            await _userRepository.Save();
        }

        public async Task<ResponseMessage> EnrollUser(EnrollmentModel model)
        {
            try
            {
                model.EntityCode = Options.EntityCode;
                var bearerRequest = await AxaMansardAuthentication();
                if (bearerRequest.Status)
                {
                    var httpClient = _httpClientFactory.CreateClient("AxaMansard");
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerRequest.Data.Auth_token);
                    HttpContent content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync($"{Options.AxaMansardEnrollement}", content);

                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        var authResponse = JsonConvert.DeserializeObject<EnrollementResponse>(apiResponse);
                        //if (authResponse.success)
                        //{
                        //    return new ResponseMessage { Status = true, Message = authResponse.message };
                        //}
                        return new ResponseMessage { Status = true, Message = authResponse.message };
                    }
                    return new ResponseMessage { Status = false, Message = "Could not connect to insurance provider. Please try again later" };
                }
                return new ResponseMessage { Status = false, Message = bearerRequest.Message };
            }
            catch(Exception ex)
            {
                _logger.LogCritical("An error occurred while enrolling user to axa-mansard", ex);
                return new ResponseMessage { Status = false, Message = "This on us.An error occurred while enrolling user to axa-mansard.Please try again later" };
            }            
        }

        private async Task<ResponseMessage<AuthenticationResponse>> AxaMansardAuthentication()
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("AxaMansard");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", $"{Options.Apikey}");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-api-secret", $"{Options.ApiSecret}");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-client-key", $"{Options.ClientKey}");

                var response = await httpClient.GetAsync($"{Options.AxaMansardToken}");
                if (response.IsSuccessStatusCode)
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    var authResponse = JsonConvert.DeserializeObject<AuthenticationResponse>(apiResponse);
                    if (authResponse.Succeeded)
                    {
                        return new ResponseMessage<AuthenticationResponse> { Data = authResponse, Status = true, Message = "Request was processed successfully" };
                    }
                    return new ResponseMessage<AuthenticationResponse> { Data = authResponse, Status = false, Message = authResponse.Message.ToString() };
                }
                return new ResponseMessage<AuthenticationResponse> { Data = null, Status = false, Message = "Could not make connection" };
            }
            catch(Exception ex)
            {
                _logger.LogCritical("Error occured while trying to get axamansard auth token",ex);
                return new ResponseMessage<AuthenticationResponse> { Data = null, Status = false, Message = "Could not make connection" };
            }

        }

        public async Task<ResponseMessage> AxaMansardGetHealthProvider(string state, string city, string healthPlan)
        {
            var hospitalList = await _hospitalListRepository.GetHealthProviders(state, city);
            return new ResponseMessage { Data = hospitalList, Status = true };
        }

        public ResponseMessage GetTowns(string state)
        {
            var townList = _hospitalListRepository.GetTowns(state).Select(x => x.City).Distinct().ToList();
            var townListDTO = new CityListDTO()
            {
                State = state,
                Cities = townList
            };
            var newList = new List<CityListDTO>();
            newList.Add(townListDTO);
            return new ResponseMessage { Data = newList, Status = true };
        }

        private string GetUniqueCode(int nCount)
        {
            string uniqueNumber = string.Empty;
            while (uniqueNumber.Length < nCount)
            {
                string varWW0 = uniqueNumber;
                Guid guid = Guid.NewGuid();
                uniqueNumber = varWW0 + RefineString(guid.ToString().Replace("-", ""));
            }

            if (uniqueNumber.Length > nCount)
            {
                uniqueNumber = uniqueNumber.Substring(0, nCount);
            }
            return uniqueNumber;
        }

        private static string RefineString(string input)
        {
            string str_ = string.Empty;
            char[] arr_ = input.ToCharArray();
            for (int i = 0; i < arr_.Length; i++)
            {
                char chr_ = arr_[i];
                if (char.IsDigit(chr_))
                {
                    str_ += chr_;
                }
            }

            return str_;
        }

        public async Task<List<AxaMansardHospitalList>> AxaHospitalList(IFormFile formFile)
        {
            var excelModels = new List<AxaMansardHospitalList>();
            using (var fileStream = new MemoryStream())
            {
                await formFile.CopyToAsync(fileStream);
                fileStream.Position = 0;
                _excelPackage.Load(fileStream);
                var ws = _excelPackage.Workbook.Worksheets[0];
                for (int r = 2; r < 2000; r++)
                {
                    if (ws.Cells[r, 3].Value?.ToString() == "" || ws.Cells[r, 3].Value?.ToString() == null)
                    {
                        break;
                    }
                    var hosiptal = new AxaMansardHospitalList();
                    hosiptal.State = ws.Cells[r, 1].Value?.ToString();
                    hosiptal.City = ws.Cells[r, 2].Value?.ToString();
                    hosiptal.HospitalName = ws.Cells[r, 3].Value?.ToString();
                    hosiptal.Address = ws.Cells[r, 4].Value?.ToString();
                    hosiptal.Specialisation = ws.Cells[r, 5].Value?.ToString();

                    excelModels.Add(hosiptal);
                }
            }
            var hospitalList = excelModels;
            _hospitalListRepository.CreateRange(hospitalList);
            await _hospitalListRepository.Save();
            return excelModels;
        }
    }
}
