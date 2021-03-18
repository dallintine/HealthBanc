using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models.Axa_Hygeia_Insurance
{
    public class ScheduledEnrollment
    {
        public ScheduledEnrollment()
        {
        }

        public ScheduledEnrollment(int insuranceUserProfileId, DateTime executionDate, string jobId, string status, string message,string insuranceService)
        {
            Id = Guid.NewGuid();
            InsuranceUserProfileId = insuranceUserProfileId;
            DateScheduled = DateTime.Now;
            ExecutionDate = executionDate;
            JobId = jobId;
            Status = status;
            Message = message;
            InsuranceService = insuranceService;
        }

        public Guid Id { get; set; }
        public int InsuranceUserProfileId { get; set;}
        public DateTime DateScheduled { get; set; }
        public DateTime ExecutionDate { get; set; }
        /// <summary>
        /// The Background Task/Job Id of scheduled job
        /// </summary>
        public string JobId { get; set; }

        public string _status;
        /// <summary>
        /// The Status of scheduled enrollment. For Possible Providers <see cref="ScheduledEnrollment_StatusValue"/>
        /// </summary>
        public string Status
        {
            get { return _status; }
            set
            {
                if (Enum.IsDefined(typeof(ScheduledEnrollment_StatusValue), value))
                {
                    _status = value;
                }
                else
                {
                    throw new ArgumentException("Value of status is not valid. Please check defined enumerated values for status in the ScheduledEnrollment_StatusValue class");
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
                    throw new ArgumentException("Value of ScheduledEnrollment.InsuranceService is not valid. Please check defined enumerated values for providers in the InsuranceProvider class");
                }
            }
        }
        public InsuranceUserProfile InsuranceUserProfile { get; set; }
    }

    // <summary>
    /// The Possible Values for <see cref="ScheduledEnrollment.Status"/>
    /// </summary>
    public enum ScheduledEnrollment_StatusValue
    {
        Processing,
        Terminated,
        Failed,
        Successful,
        Cancelled
    }
}
