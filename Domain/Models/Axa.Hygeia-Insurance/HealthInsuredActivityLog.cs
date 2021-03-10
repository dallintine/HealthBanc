using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models.Axa.Hygeia_Insurance
{
    public class HealthInsuredActivityLog
    {
        public HealthInsuredActivityLog()
        {
                
        }
        public HealthInsuredActivityLog(int? insuranceUserProfileId, int? companyProfileId, string actionApplied)
        {
            InsuranceUserProfileId = insuranceUserProfileId;
            CompanyProfileId = companyProfileId;
            ActionApplied = actionApplied;
            Date = DateTime.Now;
        }

        public int Id { get; set; }
        public int? InsuranceUserProfileId { get; set; }
        public int? CompanyProfileId { get; set; }
        public string ActionApplied { get; set; }
        public DateTime Date { get; set; }
    }
}
