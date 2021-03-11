using Domain.Models.Axa_Hygeia_Insurance;
using Domain.Models.ReportAndLogs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models.Axa.Hygeia_Insurance
{
    public class CompanyProfile
    {
        public int Id { get; set; }
        /// <summary>
        /// The ApplicationUser Id
        /// </summary>
        public int UserId { get; set; }
        /// <summary>
        /// Represent a scheduled Job/BackgroundService Id for pending insurance service Task
        /// </summary>
        public string PendingJobId { get; set; }
        public string CompanyName { get; set; }
        public string CompanyEmail { get; set; }
        public string PhoneNumber { get; set; }
        public string Industry { get; set; }
        public string CompanySize { get; set; }
        /// <summary>
        /// Boolean value to indicate if CompanyEmail has been confirmed
        /// </summary>
        public bool EmailConfirmed { get; set; }
        /// <summary>
        /// Boolean value to indicate if company profile has been completed
        /// </summary>
        public bool ProfileCompleted { get; set; }
        /// <summary>
        /// Boolean value to indicate if company first card tokenization has been completed
        /// </summary>
        public bool TokenizationCompleted { get; set; }
        /// <summary>
        /// Boolean value to indicate the company next insurance cycle premium fee
        /// </summary>
        public decimal? NextCyclePremiumFee { get; set; }
        /// <summary>
        /// Boolean value to indicate the company next insurance cycle payment date
        /// </summary>
        public DateTime NextPaymentDate { get; set; }
        /// <summary>
        /// Otp code that is used to confirm the company email
        /// </summary>
        public string OTPCode { get; set; }
        /// <summary>
        /// Background job that deletes the company otp code after specified time frame
        /// </summary>
        public string OTPJobId { get; set; }

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
                    throw new ArgumentException("Value of CompanyProfile.InsuranceService is not valid. Please check defined enumerated values for channel in the InsuranceProvider class");
                }
            }
        }
        public List<InsuranceUserProfile> InsuranceUserProfiles { get; set; }
        public List<BeneficiaryReviewUser> BeneficiaryReviewUsers { get; set; }
        public List<DebitCard> Cards { get; set; }
        public List<PaymentReference> PaymentReferences { get; set; }
        public List<ActivityLog> HealthInsuredActivityLogs { get; set; }
    }   
}
