using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models.AxaMansard_Insurance
{
    public class ScheduledPayment
    {
        public ScheduledPayment()
        {

        }
        public ScheduledPayment(int userId, int insuranceUserProfileId, Guid scheduledEnrollmentId, DateTime executionDate, string jobId, string status
            ,string message,string paymentReference,string insuranceService)
        {
            UserId = userId;
            InsuranceUserProfileId = insuranceUserProfileId;
            ScheduledEnrollmentId = scheduledEnrollmentId;
            DateScheduled = DateTime.Now;
            ExecutionDate = executionDate;
            JobId = jobId;
            Status = status;
            Message = message;
            PaymentReference = paymentReference;
            InsuranceService = insuranceService;
        }

        public int Id { get; set; }
        public int UserId { get; set; }
        public int InsuranceUserProfileId { get; set; }
        public Guid ScheduledEnrollmentId { get; set; }
        public DateTime DateScheduled { get; set; }
        public DateTime ExecutionDate { get; set; }
        public string JobId { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string PaymentReference { get; set; }
        public string InsuranceService { get; set; }
        public ScheduledEnrollment ScheduledEnrollment { get; set; }
    }
}
