using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.DTO.DashboardAnalyticsDTOs
{
    public class DashboardDTO
    {
        public int RegisteredUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public List<ServiceBreakdown> ServiceBreakdowns { get; set; }
        public List<SignUpMonth> SignUpMonths { get; set; }
    }

    public class ServiceBreakdown
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Count { get; set; }
    }

    public class SignUpMonth
    {
        public SignUpMonth(string name, int count)
        {
            Name = name;
            Count = count;
        }

        public string Name { get; set; }
        public int Count { get; set; }
    }
}
