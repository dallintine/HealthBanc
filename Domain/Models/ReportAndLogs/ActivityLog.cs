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
        public ActivityLog(int? insuranceUserProfileId, int? companyProfileId,int? familyProfileId, string actionApplied,string service)
        {
            InsuranceUserProfileId = insuranceUserProfileId;
            CompanyProfileId = companyProfileId;
            FamilyProfileId = familyProfileId;
            ActionApplied = actionApplied;
            Date = DateTime.Now;
            Service = service;
        }

        public int Id { get; set; }
        public int? InsuranceUserProfileId { get; set; }
        public int? CompanyProfileId { get; set; }
        public int? FamilyProfileId { get; set; }
        public string ActionApplied { get; set; }
        public DateTime Date { get; set; }

        public string _service;
        /// <summary>
        /// The HealthBanc Services. For Possible Providers <see cref="ServiceNames"/>
        /// </summary>
        public string Service
        {
            get { return _service; }
            set
            {
                if (Enum.IsDefined(typeof(ServiceNames), value))
                {
                    _service = value;
                }
                else
                {
                    throw new ArgumentException("Value of ActivityLog.Service is not valid. Please check defined enumerated values for providers in the ServiceNames class");
                }
            }
        }
    }
}
