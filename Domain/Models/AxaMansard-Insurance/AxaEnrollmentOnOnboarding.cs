using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models.AxaMansard_Insurance
{
    public class AxaEnrollmentOnOnboarding
    {
        public AxaEnrollmentOnOnboarding()
        {

        }

        public AxaEnrollmentOnOnboarding(int userId, int axaMansardUserProfileId, string jobId , string status, string message)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            AxaMansardUserProfileId = axaMansardUserProfileId;
            JobId = jobId;
            DateScheduled = DateTime.Now;
            Status = status;
            Message = message;
        }

        public Guid Id { get; set; }
        public int UserId { get; set; }
        public int AxaMansardUserProfileId { get; set; }
        public string JobId { get; set; }
        public DateTime DateScheduled { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public AxaMansardUserProfile AxaMansardUserProfile { get; set; }
    }
}
