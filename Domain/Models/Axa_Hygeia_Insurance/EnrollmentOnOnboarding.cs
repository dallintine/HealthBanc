using Domain.Models.Axa_Hygeia_Insurance;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models.Axa_Hygeia_Insurance
{
    public class EnrollmentOnOnboarding
    {
        public EnrollmentOnOnboarding()
        {

        }

        /// <summary>
        /// Save details on User enrollment to Health care provider.
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
       
        public string _status;
        /// <summary>
        /// Specify the enrollment status. For Possible values <see cref="EnrollmentOnOnboarding_StatusValue"/>
        /// </summary>
        public string Status
        {
            get { return _status; }
            set
            {
                if (Enum.IsDefined(typeof(EnrollmentOnOnboarding_StatusValue), value))
                {
                    _status = value;
                }
                else
                {
                    throw new ArgumentException("Value of status is not valid. Please check defined enumerated values for status in the EnrollmentOnOnboarding_StatusValue class");
                }
            }
        }
        /// <summary>
        /// Error or success message if enrollment was "failed" or "successful"
        /// </summary>
        public string Message { get; set; }

        public string _serviceused;
        /// <summary>
        /// The Insurance Service provider. For Possible Providers <see cref="InsuranceProvider"/>
        /// </summary>
        public string Serviceused
        {
            get { return _serviceused; }
            set
            {
                if (Enum.IsDefined(typeof(InsuranceProvider), value))
                {
                    _serviceused = value;
                }
                else
                {
                    throw new ArgumentException("Value of EnrollmentOnOnboarding.Serviceused is not valid. Please check defined enumerated values for channel in the InsuranceProvider class");
                }
            }
        }
        public InsuranceUserProfile InsuranceUserProfile { get; set; }
    }

    /// <summary>
    ///The Possible Values for <see cref="EnrollmentOnOnboarding.Status"/>
    /// </summary>
    public enum EnrollmentOnOnboarding_StatusValue
    {
        Failed,
        Successful
    }
}
