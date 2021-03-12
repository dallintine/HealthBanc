using Domain.Models.Axa.Hygeia_Insurance;
using Domain.Models.ReportAndLogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.Models.Axa_Hygeia_Insurance
{
    /// <summary>
    /// Save the user insurance profile details
    /// </summary>
    public class InsuranceUserProfile
    {
        public int Id { get; set; }
        /// <summary>
        /// The ApplicationUser Id
        /// </summary>
        public int UserId { get; set; }
        /// <summary>
        /// The CompanyProfile Id. This can be nullable. Its null if the user is Insurance profile is registered as an individual and not registered under a company
        /// </summary>
        public int? CompanyProfileId { get; set; }
       
        public string _companySubscribedStatus;
        /// <summary>
        /// If profile is registered under a company or group. For Possible Providers <see cref="InsuranceProfile_CompanySubStatusValue"/>
        /// </summary>
        public string CompanySubscribedStatus
        {
            get { return _companySubscribedStatus; }
            set
            {
                if (Enum.IsDefined(typeof(InsuranceProfile_CompanySubStatusValue), value))
                {
                    _companySubscribedStatus = value;
                }
                else
                {
                    throw new ArgumentException("Value of InsuranceUserProfile.CompanySubscribedStatus is not valid. Please check defined enumerated values for channel in the InsuranceProfile_CompanySubStatusValue class");
                }
            }
        }
        /// <summary>
        /// This is the Insurance Enrolle number
        /// </summary>
        public string TransId { get; set; }
        public string Gender { get; set; }
        /// <summary>
        /// Represent a scheduled Job/BackgroundService Id for pending insurance service Task
        /// </summary>
        public string PendingJobId { get; set; }
        /// <summary>
        /// Represent a scheduled Job/BackgroundService Id for scheduled email Task
        /// </summary>
        public string PendingEmailJobId { get; set; }
        public string Surname { get; set; }
        public string Othernames { get; set; }
        public string MaidenName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string ContactAddress { get; set; }
        public string Occupation { get; set; }
        public string MaritalStatus { get; set; }
        public string CareProviderName { get; set; }
        public string CPAddress { get; set; }
        public string CPCity { get; set; }
        /// <summary>
        /// Insurance PlanCode
        /// </summary>
        public string PlanCode { get; set; }
        /// <summary>
        /// Insurance Premium fee
        /// </summary>
        public Decimal Premium { get; set; }
        public string StateOfResidence { get; set; }
        public string TownOfResidence { get; set; }
        /// <summary>
        /// Represent the active status in an insurance cycle.
        /// <remarks>
        /// true = active
        /// false = inactive
        /// null = No subscription history
        /// </remarks>
        /// </summary>
        public bool? ActiveStatus { get; set; }
        /// <summary>
        /// Date Insurance cycle ends
        /// </summary>
        public DateTime EndActiveStatusDate { get; set; }
        /// <summary>
        /// Date Insurance cycle starts
        /// </summary>
        public DateTime StartActiveStatusDate { get; set; }
        /// <summary>
        /// Insurance user subscription status. 
        /// <remarks>
        /// true = active subscription
        /// false = inactive subscription
        /// null = No subscription history
        /// </remarks>
        /// </summary>
        public bool? SubscriptionStatus { get; set; }

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
                    throw new ArgumentException("Value of InsuranceUserProfile.InsuranceService is not valid. Please check defined enumerated values for providers in the InsuranceProvider class");
                }
            }
        }
        /// <summary>
        /// Date insurance profile was created
        /// </summary>
        public DateTime? DateCreated { get; set; }
        public List<DebitCard> Cards { get; set; }
        public List<PaymentReference> PaymentReferences { get; set; }
        public List<ActivityLog> HealthInsuredActivityLogs { get; set; }
    }

    // <summary>
    /// The Possible Values for <see cref="InsuranceUserProfile.CompanySubscribedStatus"/>
    /// </summary>
    public enum InsuranceProfile_CompanySubStatusValue
    {
        Active,
        Inactive,
        Pending,
    }
}
