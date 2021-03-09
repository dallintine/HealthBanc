using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models.ReportAndLogs 
{ 
    public class ActivityLog
    {
        public ActivityLog()
        {
                
        }
        public ActivityLog(int? insuranceUserProfileId, int? companyProfileId, string actionApplied,string service)
        {
            InsuranceUserProfileId = insuranceUserProfileId;
            CompanyProfileId = companyProfileId;
            ActionApplied = actionApplied;
            Date = DateTime.Now;
            Service = service;
        }

        public int Id { get; set; }
        public int? InsuranceUserProfileId { get; set; }
        public int? CompanyProfileId { get; set; }
        public string ActionApplied { get; set; }
        public DateTime Date { get; set; }
        public string Service { get; set; }
    }
}
