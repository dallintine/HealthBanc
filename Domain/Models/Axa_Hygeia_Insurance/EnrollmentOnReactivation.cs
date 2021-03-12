using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models.Axa_Hygeia_Insurance
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

        public string _status;
        /// <summary>
        /// The Status of scheduled enrollment. For Possible Providers <see cref="EnrollmentOnReactivation_StatusValue"/>
        /// </summary>
        public string Status
        {
            get { return _status; }
            set
            {
                if (Enum.IsDefined(typeof(EnrollmentOnReactivation_StatusValue), value))
                {
                    _status = value;
                }
                else
                {
                    throw new ArgumentException("Value of status is not valid. Please check defined enumerated values for status in the EnrollmentOnReactivation_StatusValue class");
                }
            }
        }
        public string Message { get; set; }

        public string _insuranceService;
        /// <summary>
        /// The Insurance Service provider. For Possible Providers <see cref="InsuranceProvider"/>
        /// </summary>
        public string InsuranceService
        {
            get { return _insuranceService; }
            set
            {
                if (Enum.IsDefined(typeof(InsuranceProvider), value))
                {
                    _insuranceService = value;
                }
                else
                {
                    throw new ArgumentException("Value of ScheduledPayment.InsuranceService is not valid. Please check defined enumerated values for providers in the InsuranceProvider class");
                }
            }
        }
        public InsuranceUserProfile InsuranceUserProfile { get; set; }
    }

    // <summary>
    /// The Possible Values for <see cref="EnrollmentOnReactivation.Status"/>
    /// </summary>
    public enum EnrollmentOnReactivation_StatusValue
    {
        Processing,
        Terminated,
        Failed,
        Successful,
        Cancelled
    }
}
