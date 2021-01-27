using Application.API_RequestModel.HealthInsured_AxaMansard;
using Application.API_RequestModel.HealthInsured_Hygeia;
using Application.API_ResponseModel.HealthInsured_Hygeia;
using Application.AuditAndReport.AuditLog;
using Application.DTO;
using Application.Helpers;
using Application.Interfaces;
using Application.ViewModels;
using Application.ViewModels.AxaMansard;
using AutoMapper;
using DataAccess.General.Interfaces;
using DataAccess.HealthInsured_AxaMansard.Interfaces;
using DataAccess.HealthInsured_Hygeia.Implementation;
using Domain.Models;
using Domain.Models.Hygeia_Insurance;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.HealthInsured_Hygeia
{
    public class Hygeia_Insurance
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IApplicationUserRepository _userRepository;
        private readonly IInsuranceCompletionProfileRepository _completionRepository;
        private readonly HygeiaUserProfileRepository _hygeiaUserProfileRepository;
        private readonly AuditLogService _auditLogServices;
        private readonly IMapper _mapper;
        private readonly IUniqueIdentifier _uniqueIdentifier;

        private HygeiaConfiguration _hygeiaAccessor { get; }


        public Hygeia_Insurance(IHttpClientFactory httpClientFactory, IOptions<HygeiaConfiguration> hygeiaAccessor, IApplicationUserRepository userRepository,
            IInsuranceCompletionProfileRepository completionRepository,HygeiaUserProfileRepository hygeiaUserProfileRepository,AuditLogService auditLogServices
            ,IMapper mapper, IUniqueIdentifier uniqueIdentifier)
        {
            _httpClientFactory = httpClientFactory;
            _userRepository = userRepository;
            _completionRepository = completionRepository;
            _hygeiaUserProfileRepository = hygeiaUserProfileRepository;
            _auditLogServices = auditLogServices;
            _mapper = mapper;
            _uniqueIdentifier = uniqueIdentifier;
            _hygeiaAccessor = hygeiaAccessor.Value;
        }

        public async Task<ResponseMessage> HygeiaOnboarding(UserProfileviewModel userProfile, int userId, string ipAddress, string device)
        {
            var user = await _userRepository.FindByIdAsync(userId);

            var checkIfUserHasBeenProfiled = await _hygeiaUserProfileRepository.GetByUserIdAsync(userId);
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

            var creatResponse = await CreateHygeiaUserProfile(userProfile, user);
            if (creatResponse.Status)
            {
                var auditViewModel = new AuditLogViewModel(userId, null, null, "Created HealthInsured profile", "Created HealthInsured profile");
                await  _auditLogServices.UserCreateAuditLog(auditViewModel, ipAddress, device);

                return new ResponseMessage
                {
                    Message = "Profile was created successfully",
                    Status = true
                };
            }
            return new ResponseMessage { Status = false, Message = "Could not create profile,please try again later" };
        }

        public async Task<ResponseMessage> CreateHygeiaUserProfile(UserProfileviewModel userProfile, ApplicationUser user)
        {
            var profile = _mapper.Map<HygeiaUserProfile>(userProfile);
            profile.UserId = user.Id;
            profile.PlanCode = _hygeiaAccessor.HygeiaPlanCode;

            profile.CareProviderName = userProfile.CareProviderName.Split(":")[0]; profile.CPAddress = userProfile.CareProviderName.Split(":")[1];
            profile.TransId = _uniqueIdentifier.GetUniqueCode(12);
            profile.CPCity = userProfile.CareProviderName.Split(":").Length == 3 ? userProfile.CareProviderName.Split(":")[2] : "";

            var updatedProfile = _mapper.Map(user, profile);

            _hygeiaUserProfileRepository.Create(updatedProfile);

            var completionProfile = new InsuranceCompletionProfile(user.Id, true, false,"Hygeia");
            _completionRepository.Create(completionProfile);
            await _completionRepository.Save();

            var newServiceString = user.ServiceUsed + "HealthInsured,";
            user.ServiceUsed = newServiceString;
            _userRepository.Update(user);
            await _userRepository.Save();
            return new ResponseMessage { Status = true };
        }

        public async Task<ResponseMessage> HygeiaRegisterUser(RegistrationModel model)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("Hygeia");
                var dictionObj = model.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(model).ToString());
                HttpContent content = new FormUrlEncodedContent(dictionObj);
                var response = await httpClient.PostAsync($"{_hygeiaAccessor.HygeiaRegistration}", content);
                if (response.IsSuccessStatusCode)
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    var authResponse = JsonConvert.DeserializeObject<RegistrationResponse>(apiResponse);
                    if (authResponse.Success)
                    {
                        return new ResponseMessage { Status = true, Message = authResponse.MemberId };
                    }
                    return new ResponseMessage { Status = true };
                }
                return new ResponseMessage { Status = false, Message = "Could not connect to insurance provider. Please try again later" };
            }
            catch (Exception ex)
            {
                //_logger.LogCritical("An error occurred while enrolling user to axa-mansard", ex);
                return new ResponseMessage { Status = false, Message = "This on us.An error occurred while enrolling user to axa-mansard.Please try again later" };
            }
        }
    }
}
