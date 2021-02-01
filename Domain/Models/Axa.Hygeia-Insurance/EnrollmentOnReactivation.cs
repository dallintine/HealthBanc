using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models.AxaMansard_Insurance
{
    public class EnrollmentOnReactivation
    {
        public EnrollmentOnReactivation()
        {

        }
        public EnrollmentOnReactivation(int userId, int insuranceUserProfileId, DateTime executionDate, string status, string message,string insuranceService)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            InsuranceUserProfileId = insuranceUserProfileId;
            DateScheduled = DateTime.Now;
            ExecutionDate = executionDate;
            Status = status;
            Message = message;
            InsuranceService = insuranceService;
        }

        public Guid Id { get; set; }
        public int UserId { get; set; }
        public int InsuranceUserProfileId { get; set; }
        public DateTime DateScheduled { get; set; }
        public DateTime ExecutionDate { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string InsuranceService { get; set; }
        public InsuranceUserProfile InsuranceUserProfile { get; set; }
    }
}
