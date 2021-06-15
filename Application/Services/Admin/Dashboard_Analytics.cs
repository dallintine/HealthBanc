using Application.DTO;
using Application.DTO.DashboardAnalyticsDTOs;
using AutoMapper;
using DataAccess;
using DataAccess.General.Interfaces;
using DataAccess.HealthInsured.Interfaces;
using Domain.Models;
using Domain.Models.Axa_Hygeia_Insurance;
using HealthBanc.DTO.DashboardAnalyticsDTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Admin
{
    public class Dashboard_Analytics
    {
        private readonly IMapper _mapper;
        private readonly IRepositoryWrapper _repowrapper;

        public Dashboard_Analytics(IMapper mapper,IRepositoryWrapper repowrapper)
        {
            _mapper = mapper;
            _repowrapper = repowrapper;
        }

        public async Task<DashboardDTO> GetUsersStatus()
        {
            // Gets a IQuerayble of all application users
            var users = _repowrapper.ApplicationUser.GetQueraybaleUser();

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
            var users = _repowrapper.ApplicationUser.GetQueraybaleUser();

            //Gets list of all present Healthbanc service
            var services = await _repowrapper.Service.GetServicesAsync();

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
            var dashboardDTO = new DashboardDTO
            {
                ServiceBreakdowns = dashboardServiceList
            };
            return dashboardDTO;
        }

        public async Task<DashboardDTO> GetSignUpAnalytics(int? Id)
        {
            // Gets a IQuerayble of all application users
            var users = _repowrapper.ApplicationUser.GetQueraybaleUser();

            string[] months = new string[] { "Janaury", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
            var count = 1;
            var dashboardDTO = new DashboardDTO
            {
                // List of users with monthly signUp details {Name,Count}
                SignUpMonths = new List<SignUpMonth>()
            };


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

        public async Task<ResponseMessage> GetHealthInsuredDashBoardAnalytics(int? insuranceServiceId)
        {
            var users =  _repowrapper.InsuranceProfile.QueryAllInsuranceProfiles();
            var paymentReferences = _repowrapper.PaymentReference.QueryAllPaymentReference();

            var totalUsers = 0;
            var totalRevenue = decimal.Parse("0");
            var revenueDue = decimal.Parse("0");
            var payoutDue = decimal.Parse("0");
            if (insuranceServiceId is null)
            {
                totalUsers = await users.CountAsync();
                totalRevenue = paymentReferences.Where(x => x.Status.Equals(PaymentReference_StatusValue.Successful.ToString())
                && x.Date.Date.Month == DateTime.Now.Date.Month).Select(x => x.Amount).Sum();
                payoutDue = totalRevenue * decimal.Parse("0.9");
            }
            // for hygeia
            if(insuranceServiceId == 1)
            {
                totalUsers = await users.Where(x => x.InsuranceService.Equals(InsuranceProvider.Hygeia.ToString())).CountAsync();
                totalRevenue = paymentReferences.Where(x => x.Status.Equals(PaymentReference_StatusValue.Successful.ToString()) && x.Amount > decimal.Parse("50")
                && x.Channel.Equals(PaymentReference_ChannelValue.healthinsured_hygeia.ToString()) && x.Date.Date.Month == DateTime.Now.Date.Month)
                    .Select(x => x.Amount).Sum();
                revenueDue = users.Where(x => x.InsuranceService.Equals(InsuranceProvider.Hygeia.ToString()) && x.SubscriptionStatus == true)
                    .Select(x => x.Premium).Sum();
            }
            if(insuranceServiceId == 2)
            {
                totalUsers = await users.Where(x => x.InsuranceService.Equals(InsuranceProvider.Axamansard.ToString())).CountAsync();
                totalRevenue = paymentReferences.Where(x => x.Status.Equals(PaymentReference_StatusValue.Successful.ToString()) && x.Amount > decimal.Parse("50")
                && x.Channel.Equals(PaymentReference_ChannelValue.healthinsured_axamansard.ToString()) && x.Date.Date.Month == DateTime.Now.Date.Month)
                    .Select(x => x.Amount).Sum();
                revenueDue = users.Where(x => x.InsuranceService.Equals(InsuranceProvider.Axamansard.ToString()) && x.SubscriptionStatus == true).Select(x => x.Premium).Sum();
            }
            var healthInsuredDashboard = new HealthInsuredDashboardDTO()
            {
                TotalUser = totalUsers,
                ToatlRevenue = totalRevenue,
                TotalPayoutDue = payoutDue,
                RevenueDue = revenueDue
            };
            return new ResponseMessage { Data = healthInsuredDashboard , Status= true, Message="Dashboard analytics was fectched successfully"};
        }

        public async Task<ResponseMessage> HealthInsuredUserAcquisition(int? Id)
        {
            // Gets a IQuerayble of all insurance users
            var users = _repowrapper.InsuranceProfile.QueryAllInsuranceProfiles();

            string[] months = new string[] { "Janaury", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
            string[] days = new string[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
            string[] weeks = new string[] { "First Week", "Second Week", "Third Week", "Fourth Week" };
            var count = 1;
            var healthInsuredDashboardDTO = new HealthInsuredDashboardDTO
            {
                // List of users with monthly signUp details {Name,Count}
                UserAcquisitions = new List<UserAcquisition>()
            };


            // If Id is null, returns a list showing the number of users registered in a particular month of present year
            if (Id is null)
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
            // If Id is 1, returns a list showing the number of users registered in a particular month of present year
            else if (Id == 1)
            {
                foreach (var item in weeks)
                {
                    var userRegisteredInParticularMonth = await users.Where(x => x.DateCreated.Value
                    .Year == DateTime.Now.Year && x.DateCreated.Value.Month == DateTime.Now.Month).ToListAsync();

                    var userRegisterdInWeek = ProcessUserInMonth(userRegisteredInParticularMonth, count);

                    var userAcquisition = new UserAcquisition(userRegisterdInWeek.Name, userRegisterdInWeek.Count);
                    healthInsuredDashboardDTO.UserAcquisitions.Add(userAcquisition);
                    count++;
                }
            }
            // If Id is 2, returns a list showing the number of users registered in a present week of year
            else
            {
                var startOfWeek = processStartOfPresentWeek(DateTime.Now);
                var endOfWeek = startOfWeek.AddDays(7);
                foreach (var item in days)
                {
                    var userRegisterdInPresentWeek = await users.Where(x => x.DateCreated.Value.Date >= startOfWeek.Date && x.DateCreated.Value.Date < endOfWeek.Date).ToListAsync();

                    var userRegisteredInDay = ProcessUserInWeek(userRegisterdInPresentWeek, count, startOfWeek);

                    var userAcquisition = new UserAcquisition(userRegisteredInDay.Name, userRegisteredInDay.Count);
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
            string[] days = new string[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"};
            string[] weeks = new string[] { "First Week", "Second Week", "Third Week", "Fourth Week" };
            var count = 1;
            var healthInsuredDashboardDTO = new HealthInsuredDashboardDTO
            {

                // List of users with monthly signUp details {Name,Count}
                SubscriberAcquisitions = new List<SubscriberAcquisition>()
            };


            // If Id is null, returns a list showing the number of users subscribed in a present year
            if (Id is null)
            {
                foreach (var item in months)
                {
                    var userSubscribedInParticularMonth = await users.Where(x => x.DateCreated.Value
                    .Year == DateTime.Now.Year && x.DateCreated.Value.Month == count && x.SubscriptionStatus != null).CountAsync();
                    var userAcquisition = new SubscriberAcquisition(item, userSubscribedInParticularMonth);
                    healthInsuredDashboardDTO.SubscriberAcquisitions.Add(userAcquisition);
                    count++;
                }
            }
            // If Id is 1, returns a list showing the number of users subscribed in a month of present year
            else if (Id == 1)
            {
                foreach (var item in weeks)
                {
                    var userSubscribedInParticularMonth = await users.Where(x => x.DateCreated.Value
                    .Year == DateTime.Now.Year && x.DateCreated.Value.Month == DateTime.Now.Month && x.SubscriptionStatus != null).ToListAsync();

                    var usersubscribedInWeek = ProcessUserInMonth(userSubscribedInParticularMonth,count);
                    healthInsuredDashboardDTO.SubscriberAcquisitions.Add(usersubscribedInWeek);
                    count++;
                }
            }
            // If Id is 2, returns a list showing the number of users subscribed in a present week of year
            else
            {
                var startOfWeek = processStartOfPresentWeek(DateTime.Now);
                var endOfWeek = startOfWeek.AddDays(7);
                foreach (var item in days)
                {
                    var userSubscribedInPresentWeek = await users.Where(x => x.DateCreated.Value.Date >= startOfWeek.Date && x.DateCreated.Value.Date < endOfWeek.Date && x.SubscriptionStatus != null).ToListAsync();

                    var userAcquisition = ProcessUserInWeek(userSubscribedInPresentWeek, count, startOfWeek);
                    healthInsuredDashboardDTO.SubscriberAcquisitions.Add(userAcquisition);
                    count++;
                }
            }
            return new ResponseMessage { Data = healthInsuredDashboardDTO, Status = true, Message = "Subscriber acquisitions was fecthed successfully" };            
        }

        private DateTime processStartOfPresentWeek(DateTime date)
        {
            DateTime dt = date;

            bool isSunday = dt.DayOfWeek == 0;
            var dayOfweek = isSunday == false ? (int)dt.DayOfWeek : 7;

            DateTime startOfWeek = dt.AddDays(((int)(dayOfweek) * -1) + 1);
            return startOfWeek;
        }

        private SubscriberAcquisition ProcessUserInWeek(List<InsuranceUserProfile> insuranceProfiles, int count, DateTime weekStart)
        {            
            if (count is 1)
            {
                var userCountMonday = insuranceProfiles.Where(x => x.DateCreated.Value.Date == weekStart.Date).Count();
                return new SubscriberAcquisition("Monday", userCountMonday);
            }
            else if (count is 2)
            {
                var userCountTuesday = insuranceProfiles.Where(x => x.DateCreated.Value.Date == weekStart.AddDays(1).Date).Count();
                return new SubscriberAcquisition("Tuesday", userCountTuesday);

            }
            else if (count is 3)
            {
                var userCountWednesday = insuranceProfiles.Where(x => x.DateCreated.Value.Date >= weekStart.AddDays(2).Date).Count();
                return new SubscriberAcquisition("Wednesday", userCountWednesday);
            }
            else if (count is 4)
            {
                var userCountThursday = insuranceProfiles.Where(x => x.DateCreated.Value.Date >= weekStart.AddDays(3).Date).Count();
                return new SubscriberAcquisition("Thursday", userCountThursday);
            }
            else if (count is 5)
            {
                var userCountFriday = insuranceProfiles.Where(x => x.DateCreated.Value.Date >= weekStart.AddDays(4).Date).Count();
                return new SubscriberAcquisition("Friday", userCountFriday);
            }
            else if (count is 6)
            {
                var userCountSaturday = insuranceProfiles.Where(x => x.DateCreated.Value.Date >= weekStart.AddDays(5).Date).Count();
                return new SubscriberAcquisition("Saturday", userCountSaturday);
            }
            else
            {
                var userCountSunday= insuranceProfiles.Where(x => x.DateCreated.Value.Date >= weekStart.AddDays(6).Date).Count();
                return new SubscriberAcquisition("Sunday", userCountSunday);
            }
        }

        private SubscriberAcquisition ProcessUserInMonth(List<InsuranceUserProfile> insuranceProfiles,int count)
        {
            var presentDay = DateTime.Now;
            var presentMonth = new DateTime(presentDay.Year, presentDay.Month, 1);
            var firstWeekOfMonth = presentMonth;
            var secondWeekOfMonth = firstWeekOfMonth.AddDays(7);
            var thirdWeekOfMonth = secondWeekOfMonth.AddDays(7);
            var fourthWeekOfMonth = thirdWeekOfMonth.AddDays(7);

            if(count is 1)
            {
                var userCountFirstWeek = insuranceProfiles.Where(x => x.DateCreated.Value < secondWeekOfMonth).Count();
                return new SubscriberAcquisition("First Week",userCountFirstWeek);
            }
            else if(count is 2)
            {
                var userCountSecondWeek = insuranceProfiles.Where(x => x.DateCreated.Value >= secondWeekOfMonth && x.DateCreated.Value < thirdWeekOfMonth).Count();
                return new SubscriberAcquisition("Second Week", userCountSecondWeek);

            }
            else if (count is 3)
            {
                var userCountThirdWeek = insuranceProfiles.Where(x => x.DateCreated.Value >= thirdWeekOfMonth && x.DateCreated.Value < fourthWeekOfMonth).Count();
                return new SubscriberAcquisition("Third Week", userCountThirdWeek);

            }
            else
            {
                var userCountFourthWeek = insuranceProfiles.Where(x => x.DateCreated.Value >= fourthWeekOfMonth).Count();
                return new SubscriberAcquisition("Fourth Week", userCountFourthWeek);
            }
        }
    }
}












































//atediff(week, @dateOne, @dateTwo) as weekdiff


//if (week == 1)
//{
//    foreach (var item in insuranceProfiles)
//    {

//    }
//    var cal = System.Globalization.DateTimeFormatInfo.CurrentInfo.Calendar;
//    var d1 = firstWeekOfMonth.Date.AddDays(-1 * (int)cal.GetDayOfWeek(firstWeekOfMonth));
//    var d2 = date.Date.AddDays(-1 * (int)cal.GetDayOfWeek(date));

//    return d1 == d2;
//}
//else if(week == 2)
//{
//    var cal = System.Globalization.DateTimeFormatInfo.CurrentInfo.Calendar;
//    var d1 = secondWeekOfMonth.Date.AddDays(-1 * (int)cal.GetDayOfWeek(secondWeekOfMonth));
//    var d2 = date.Date.AddDays(-1 * (int)cal.GetDayOfWeek(date));

//    return d1 == d2;
//}
//else if (week == 3)
//{
//    var cal = System.Globalization.DateTimeFormatInfo.CurrentInfo.Calendar;
//    var d1 = thirdWeekOfMonth.Date.AddDays(-1 * (int)cal.GetDayOfWeek(thirdWeekOfMonth));
//    var d2 = date.Date.AddDays(-1 * (int)cal.GetDayOfWeek(date));

//    return d1 == d2;
//}
//else
//{
//    var cal = System.Globalization.DateTimeFormatInfo.CurrentInfo.Calendar;
//    var d1 = fourthWeekOfMonth.Date.AddDays(-1 * (int)cal.GetDayOfWeek(fourthWeekOfMonth));
//    var d2 = date.Date.AddDays(-1 * (int)cal.GetDayOfWeek(date));

//    return d1 == d2;
//}