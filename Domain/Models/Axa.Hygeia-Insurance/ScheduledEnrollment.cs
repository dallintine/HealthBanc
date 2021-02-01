using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models.AxaMansard_Insurance
{
    public class ScheduledEnrollment
    {
        public ScheduledEnrollment()
        {
        }

        public ScheduledEnrollment(int userId, int insuranceUserProfileId, DateTime executionDate, string jobId, string status, string message,string insuranceService)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            InsuranceUserProfileId = insuranceUserProfileId;
            DateScheduled = DateTime.Now;
            ExecutionDate = executionDate;
            JobId = jobId;
            Status = status;
            Message = message;
            InsuranceService = insuranceService;
        }

        public Guid Id { get; set; }
        public int UserId { get; set; }
        public int InsuranceUserProfileId { get; set;}
        public DateTime DateScheduled { get; set; }
        public DateTime ExecutionDate { get; set; }
        public string JobId { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string InsuranceService { get; set; }
        public InsuranceUserProfile InsuranceUserProfile { get; set; }
    }
}
