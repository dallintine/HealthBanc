using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models.AxaMansard_Insurance
{
    public class EnrollmentOnOnboarding
    {
        public EnrollmentOnOnboarding()
        {

        }

        public EnrollmentOnOnboarding(int userId, int insuranceUserProfileId, string jobId , string status, string message,string insuranceService)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            InsuranceUserProfileId = insuranceUserProfileId;
            JobId = jobId;
            DateScheduled = DateTime.Now;
            Status = status;
            Message = message;
            InsuranceService = insuranceService;

        }

        public Guid Id { get; set; }
        public int UserId { get; set; }
        public int InsuranceUserProfileId { get; set; }
        public string JobId { get; set; }
        public DateTime DateScheduled { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string Serviceused { get; set; }
        public string InsuranceService { get; set; }
        public InsuranceUserProfile InsuranceUserProfile { get; set; }
    }
}
