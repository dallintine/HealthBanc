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

        /// <summary>
        /// Save details on User enrollment to Health care provider. Health care is either hygeia or axamansard
        /// For now we saving only failed enrollments
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="insuranceUserProfileId"></param>
        /// <param name="status"></param>
        /// <param name="message"></param>
        /// <param name="serviceUsed"></param>
        public EnrollmentOnOnboarding(int userId, int insuranceUserProfileId, string status, string message,string serviceUsed)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            InsuranceUserProfileId = insuranceUserProfileId;
            DateScheduled = DateTime.Now;
            Status = status;
            Message = message;
            Serviceused = serviceUsed;
        }

        public Guid Id { get; set; }
        /// <summary>
        /// ApplicationUser Id 
        /// </summary>
        public int UserId { get; set; }
        /// <summary>
        /// InsuranceUser Profile Id
        /// </summary>
        public int InsuranceUserProfileId { get; set; }
        /// <summary>
        /// Date Enrollment was carried out
        /// </summary>
        public DateTime DateScheduled { get; set; }
        /// <summary>
        /// Specify Maybe the enrollment was : "failed","successful"
        /// </summary>
        public string Status { get; set; }
        /// <summary>
        /// Error or success message if enrollment was "failed" or "successful"
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// Refers to the service : Can either be hygeia or axamansard
        /// </summary>
        public string Serviceused { get; set; }
        public InsuranceUserProfile InsuranceUserProfile { get; set; }
    }
}
