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
        public PaymentOnReactivation(int userId, int axaMansardUserProfileId,Guid axaEnrollmentOnReactivationId,DateTime executionDate, string status, string jobId,
            string message,string paymentReference)
        {
            UserId = userId;
            AxaMansardUserProfileId = axaMansardUserProfileId;
            AxaEnrollmentOnReactivationId = axaEnrollmentOnReactivationId;
            DateScheduled = DateTime.Now;
            ExecutionDate = executionDate;
            Status = status;
            Message = message;
            JobId = jobId;
            Date = DateTime.Now;
            PaymentReference = paymentReference;
        }
        public int Id { get; set; }
        public int UserId { get; set; }
        public int AxaMansardUserProfileId { get; set; }
        public Guid AxaEnrollmentOnReactivationId { get; set; }
        public DateTime DateScheduled { get; set; }
        public DateTime ExecutionDate { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string JobId { get; set; }
        public DateTime Date { get; set; }
        public string PaymentReference { get; set; }
        public AxaEnrollmentOnReactivation AxaEnrollmentOnReactivation { get; set; }
        public AxaMansardUserProfile AxaMansardUserProfile { get; set; }
    }
}
