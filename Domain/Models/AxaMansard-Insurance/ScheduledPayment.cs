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
        public ScheduledPayment(int userId, int axaMansardUserProfileId, Guid scheduledAxaEnrollmentId, DateTime executionDate, string jobId, string status,string message)
        {
            UserId = userId;
            AxaMansardUserProfileId = axaMansardUserProfileId;
            ScheduledAxaEnrollmentId = scheduledAxaEnrollmentId;
            DateScheduled = DateTime.Now;
            ExecutionDate = executionDate;
            JobId = jobId;
            Status = status;
            Message = message;
        }

        public int Id { get; set; }
        public int UserId { get; set; }
        public int AxaMansardUserProfileId { get; set; }
        public Guid ScheduledAxaEnrollmentId { get; set; }
        public DateTime DateScheduled { get; set; }
        public DateTime ExecutionDate { get; set; }
        public string JobId { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public ScheduledAxaEnrollment ScheduledAxaEnrollment { get; set; }
        public AxaMansardUserProfile AxaMansardUserProfile { get; set; }
    }
}
