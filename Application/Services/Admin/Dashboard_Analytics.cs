using Application.DTO;
using Application.DTO.DashboardAnalyticsDTOs;
using AutoMapper;
using DataAccess;
using DataAccess.General.Interfaces;
using DataAccess.HealthInsured.Interfaces;
using Domain.Models;
using HealthBanc.DTO.DashboardAnalyticsDTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Admin
{
    public class Dashboard_Analytics
    {
        private readonly IMapper _mapper;
        private readonly IApplicationUserRepository _userRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly IRepositoryWrapper _repowrapper;

        public Dashboard_Analytics(IMapper mapper, IApplicationUserRepository userRepository,IServiceRepository serviceRepository,IRepositoryWrapper repowrapper)
        {
            _mapper = mapper;
            _userRepository = userRepository;
            _serviceRepository = serviceRepository;
            _repowrapper = repowrapper;
        }

        public async Task<DashboardDTO> GetUsersStatus()
        {
            // Gets a IQuerayble of all application users
            var users = _userRepository.GetQueraybaleUser();

            // Gets a count of all application users
            var registeredUsers = await users.CountAsync();

            // Gets all active users that have logged in in the past 30 days. 
            var activeUsers = await users.Where(x => x.LastLoginDate.AddDays(30) >= DateTime.Now).CountAsync();

            //Gets all inactive active users by subtracting active users from registered users.
            var inactiveUsers = registeredUsers - activeUsers;

            var dashboardDTO = new DashboardDTO
            {
                RegisteredUsers = registeredUsers,
                ActiveUsers = activeUsers,
                InactiveUsers = inactiveUsers
            };
            return dashboardDTO;
        }

        public async Task<DashboardDTO> GetServiceBreakdown(int? Id)
        {
            // Gets a IQuerayble of all application users
            var users = _userRepository.GetQueraybaleUser();

            //Gets list of all present Healthbanc service
            var services = await _serviceRepository.GetServicesAsync();

            // Maps the present Healthbanc services to a Service Breakdown DTO ( {Id,Name,Count} )
            var dashboardServiceList = _mapper.Map<List<Service>, List<ServiceBreakdown>>(services);

            // If Id is null returns users service used breakdown  for all time
            if (Id is null)
            {
                foreach (var item in dashboardServiceList)
                {
                    item.Count = await users.Where(x => x.ServiceUsed.Contains(item.Name.ToString())).CountAsync();
                }
            }
            // If Id is 1 return users service used breakdown for present year
            else if (Id == 1)
            {
                foreach (var item in dashboardServiceList)
                {
                    item.Count = await users.Where(x => x.ServiceUsed.Contains(item.Name.ToString()) && x.DateOfRegistration.Year == DateTime.Now.Year).CountAsync();
                }
            }
            //If Id is 2 returns users service used breakdown for present month
            else if (Id == 2)
            {
                foreach (var item in dashboardServiceList)
                {
                    item.Count = await users.Where(x => x.ServiceUsed.Contains(item.Name.ToString()) && x.DateOfRegistration.Month == DateTime.Now.Month).CountAsync();
                }
            }
            var dashboardDTO = new DashboardDTO();
            dashboardDTO.ServiceBreakdowns = dashboardServiceList;
            return dashboardDTO;
        }

        public async Task<DashboardDTO> GetSignUpAnalytics(int? Id)
        {
            // Gets a IQuerayble of all application users
            var users = _userRepository.GetQueraybaleUser();

            string[] months = new string[] { "Janaury", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
            var count = 1;
            var dashboardDTO = new DashboardDTO();

            // List of users with monthly signUp details {Name,Count}
            dashboardDTO.SignUpMonths = new List<SignUpMonth>();


            // If Id is null, returns a list showing the number of users registered in a particular month for all years
            if (Id is null)
            {
                foreach (var item in months)
                {
                    var userRegisteredInParticularMonth = await users.Where(x => x.DateOfRegistration.Month == count).CountAsync();
                    var signUpMonth = new SignUpMonth(item, userRegisteredInParticularMonth);
                    dashboardDTO.SignUpMonths.Add(signUpMonth);
                    count++;
                }
            }
            // If Id is 1, returns a list showing the number of users registered in a particular month of present year
            else if (Id == 1)
            {
                foreach (var item in months)
                {
                    var userRegisteredInParticularMonth = await users.Where(x => x.DateOfRegistration.Year == DateTime.Now.Year &&
                    x.DateOfRegistration.Month == count).CountAsync();
                    var signUpMonth = new SignUpMonth(item, userRegisteredInParticularMonth);
                    dashboardDTO.SignUpMonths.Add(signUpMonth);
                    count++;
                }
            }

            return dashboardDTO;
        }

        public async Task<DashboardDTO> GetAllDashboardAnalystics()
        {
            var usersStatus = await GetUsersStatus();
            var signUpAnalytics = await GetSignUpAnalytics(null);
            var serviceBreakdown = await GetServiceBreakdown(null);
            return new DashboardDTO()
            {
                ActiveUsers = usersStatus.ActiveUsers,
                RegisteredUsers = usersStatus.RegisteredUsers,
                InactiveUsers = usersStatus.InactiveUsers,
                ServiceBreakdowns = serviceBreakdown.ServiceBreakdowns,
                SignUpMonths = signUpAnalytics.SignUpMonths
            };
        }

        public async Task<ResponseMessage> GetHealthInsuredDashBoardAnalytics()
        {
            var users =  _repowrapper.InsuranceProfile.QueryAllInsuranceProfiles();
            var totalUsers = await users.CountAsync();
            var paymentReferences = _repowrapper.PaymentReference.QueryAllPaymentReference();
            var totalRevenue = paymentReferences.Where(x => x.Status.ToLower().Trim() == "successful").Select(x => x.Amount).Sum();
            var healthInsuredDashboard = new HealthInsuredDashboardDTO()
            {
                TotalUser = totalUsers,
                ToatlRevenue = totalRevenue,
                TotalPayoutDue = 0
            };
            return new ResponseMessage { Data = healthInsuredDashboard , Status= true, Message="Dashboard analytics was fectched successfully"};
        }

        public async Task<ResponseMessage> HealthInsuredUserAcquisition(int? Id)
        {
            // Gets a IQuerayble of all insurance users
            var users = _repowrapper.InsuranceProfile.QueryAllInsuranceProfiles();

            string[] months = new string[] { "Janaury", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
            var count = 1;
            var healthInsuredDashboardDTO = new HealthInsuredDashboardDTO();

            // List of users with monthly signUp details {Name,Count}
            healthInsuredDashboardDTO.UserAcquisitions = new List<UserAcquisition>();


            // If Id is null, returns a list showing the number of users registered in a particular month for all years
            if (Id is null)
            {
                foreach (var item in months)
                {
                    var userRegisteredInParticularMonth = await users.Where(x => x.DateCreated.Value.Month == count).CountAsync();
                    var userAcquisition = new UserAcquisition(item, userRegisteredInParticularMonth);
                    healthInsuredDashboardDTO.UserAcquisitions.Add(userAcquisition);
                    count++;
                }
            }
            // If Id is 1, returns a list showing the number of users registered in a particular month of present year
            else if (Id == 1)
            {
                foreach (var item in months)
                {
                    var userRegisteredInParticularMonth = await users.Where(x => x.DateCreated.Value
                    .Year == DateTime.Now.Year && x.DateCreated.Value.Month == count).CountAsync();

                    var userAcquisition = new UserAcquisition(item, userRegisteredInParticularMonth);
                    healthInsuredDashboardDTO.UserAcquisitions.Add(userAcquisition);
                    count++;
                }
            }

            return new ResponseMessage { Data = healthInsuredDashboardDTO, Status = true, Message = "User acquisitions was fecthed successfully" };
        }

        public async Task<ResponseMessage> HealthInsuredSubscriberAcquisition(int? Id)
        {
            // Gets a IQuerayble of all insurance users
            var users = _repowrapper.InsuranceProfile.QueryAllInsuranceProfiles();

            string[] months = new string[] { "Janaury", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
            var count = 1;
            var healthInsuredDashboardDTO = new HealthInsuredDashboardDTO();

            // List of users with monthly signUp details {Name,Count}
            healthInsuredDashboardDTO.SubscriberAcquisitions = new List<SubscriberAcquisition>();


            // If Id is null, returns a list showing the number of users registered in a particular month for all years
            if (Id is null)
            {
                foreach (var item in months)
                {
                    var userRegisteredInParticularMonth = await users.Where(x => x.DateCreated.Value.Month == count && x.SubscriptionStatus == true).CountAsync();
                    var userAcquisition = new SubscriberAcquisition(item, userRegisteredInParticularMonth);
                    healthInsuredDashboardDTO.SubscriberAcquisitions.Add(userAcquisition);
                    count++;
                }
            }
            // If Id is 1, returns a list showing the number of users registered in a particular month of present year
            else if (Id == 1)
            {
                foreach (var item in months)
                {
                    var userRegisteredInParticularMonth = await users.Where(x => x.DateCreated.Value
                    .Year == DateTime.Now.Year && x.DateCreated.Value.Month == count && x.SubscriptionStatus == true).CountAsync();

                    var userAcquisition = new SubscriberAcquisition(item, userRegisteredInParticularMonth);
                    healthInsuredDashboardDTO.SubscriberAcquisitions.Add(userAcquisition);
                    count++;
                }
            }
            return new ResponseMessage { Data = healthInsuredDashboardDTO, Status = true, Message = "Subscriber acquisitions was fecthed successfully" };
        }
    }
}
