using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models.Axa_Hygeia_Insurance
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

        public string _status;
        /// <summary>
        /// The Status of scheduled payment. For Possible Providers <see cref="PaymentOnReactivation_StatusValue"/>
        /// </summary>
        public string Status
        {
            get { return _status; }
            set
            {
                if (Enum.IsDefined(typeof(PaymentOnReactivation_StatusValue), value))
                {
                    _status = value;
                }
                else
                {
                    throw new ArgumentException("Value of status is not valid. Please check defined enumerated values for status in the PaymentOnReactivation_StatusValue class");
                }
            }
        }
        public string Message { get; set; }
        public string JobId { get; set; }
        public DateTime Date { get; set; }
        public string PaymentReference { get; set; }

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
        public EnrollmentOnReactivation EnrollmentOnReactivation { get; set; }
    }

    // <summary>
    /// The Possible Values for <see cref="ScheduledPayment.Status"/>
    /// </summary>
    public enum PaymentOnReactivation_StatusValue
    {
        Processing,
        Terminated,
        Failed,
        Successful,
        Cancelled
    }
}
