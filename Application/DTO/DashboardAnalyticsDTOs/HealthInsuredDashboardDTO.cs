using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTO.DashboardAnalyticsDTOs
{
    public class HealthInsuredDashboardDTO
    {
        public int TotalUser { get; set; }
        public decimal ToatlRevenue { get; set; }
        public decimal TotalPayoutDue { get; set; }
        public decimal RevenueDue { get; set; }
        public List<UserAcquisition> UserAcquisitions { get; set; }
        public List<SubscriberAcquisition> SubscriberAcquisitions { get; set; }
    }

    public class UserAcquisition
    {
        public UserAcquisition(string name, int count)
        {
            Name = name;
            Count = count;
        }

        public string Name { get; set; }
        public int Count { get; set; }
    }

    public class SubscriberAcquisition
    {
        public SubscriberAcquisition(string name, int count)
        {
            Name = name;
            Count = count;
        }

        public string Name { get; set; }
        public int Count { get; set; }
    }
}
