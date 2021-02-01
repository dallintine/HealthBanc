using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models.AxaMansard_Insurance
{
    public class PaymentOnReactivation
    {
        public PaymentOnReactivation()
        {

        }
        public PaymentOnReactivation(int userId, int insuranceUserProfileId, Guid enrollmentOnReactivationId, DateTime executionDate, string status, string jobId,
            string message,string paymentReference,string insuranceService)
        {
            UserId = userId;
            InsuranceUserProfileId = insuranceUserProfileId;
            EnrollmentOnReactivationId = enrollmentOnReactivationId;
            DateScheduled = DateTime.Now;
            ExecutionDate = executionDate;
            Status = status;
            Message = message;
            JobId = jobId;
            Date = DateTime.Now;
            PaymentReference = paymentReference;
            InsuranceService = insuranceService;
        }
        public int Id { get; set; }
        public int UserId { get; set; }
        public int InsuranceUserProfileId { get; set; }
        public Guid EnrollmentOnReactivationId { get; set; }
        public DateTime DateScheduled { get; set; }
        public DateTime ExecutionDate { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string JobId { get; set; }
        public DateTime Date { get; set; }
        public string PaymentReference { get; set; }
        public string InsuranceService { get; set; }
        public EnrollmentOnReactivation EnrollmentOnReactivation { get; set; }
    }
}
