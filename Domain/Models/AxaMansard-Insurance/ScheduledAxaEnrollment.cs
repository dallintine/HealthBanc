using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models.AxaMansard_Insurance
{
    public class ScheduledAxaEnrollment
    {
        public ScheduledAxaEnrollment()
        {
        }

        public ScheduledAxaEnrollment(int userId, int axaMansardUserProfileId,DateTime executionDate, string jobId, string status, string message)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            AxaMansardUserProfileId = axaMansardUserProfileId;
            DateScheduled = DateTime.Now;
            ExecutionDate = executionDate;
            JobId = jobId;
            Status = status;
            Message = message;
        }

        public Guid Id { get; set; }
        public int UserId { get; set; }
        public int AxaMansardUserProfileId { get; set;}
        public DateTime DateScheduled { get; set; }
        public DateTime ExecutionDate { get; set; }
        public string JobId { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public AxaMansardUserProfile AxaMansardUserProfile { get; set; }
    }
}
