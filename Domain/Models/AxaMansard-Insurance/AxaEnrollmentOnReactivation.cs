using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models.AxaMansard_Insurance
{
    public class AxaEnrollmentOnReactivation
    {
        public AxaEnrollmentOnReactivation()
        {

        }
        public AxaEnrollmentOnReactivation(int userId, int axaMansardUserProfileId,DateTime executionDate, string status, string message)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            AxaMansardUserProfileId = axaMansardUserProfileId;
            DateScheduled = DateTime.Now;
            ExecutionDate = executionDate;
            Status = status;
            Message = message;
        }

        public Guid Id { get; set; }
        public int UserId { get; set; }
        public int AxaMansardUserProfileId { get; set; }
        public DateTime DateScheduled { get; set; }
        public DateTime ExecutionDate { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public AxaMansardUserProfile AxaMansardUserProfile { get; set; }
    }
}
