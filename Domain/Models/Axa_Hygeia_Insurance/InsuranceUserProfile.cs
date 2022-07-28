using Domain.Models.Axa.Hygeia_Insurance;
using Domain.Models.ReportAndLogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Domain.Models.Axa_Hygeia_Insurance
{
    /// <summary>
    /// Save the user insurance profile details
    /// </summary>
    public class InsuranceUserProfile
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public int? CompanyProfileId { get; set; }
        public string CompanyName { get; set; }
        public string CompanySubscribedStatus { get; set; }
        public int? FamilyProfileId { get; set; }
        public string FamilyEmail { get; set; }
        public int? InsurancePayeeId { get; set; }
        public string AxamasardReferenceCode { get; set; }
        public string TransId { get; set; }
        public string Gender { get; set; }
        public string PendingJobId { get; set; }
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
        public string Image { get; set; }
        public string PlanCode { get; set; }
        public Decimal Premium { get; set; }
        public string StateOfResidence { get; set; }
        public string TownOfResidence { get; set; }
        /// <summary>
        /// Number of times scheduled payment Fails
        /// </summary>
        public int? FailedScheduledPaymentRetry { get; set; }
        public bool? ActiveStatus { get; set; }
        public DateTime EndActiveStatusDate { get; set; }
        public DateTime StartActiveStatusDate { get; set; }
        public bool? SubscriptionStatus { get; set; }
        public string _insuranceService;
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
        public DateTime? DateCreated { get; set; }
        public List<DebitCard> Cards { get; set; }
        [JsonIgnore]
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
