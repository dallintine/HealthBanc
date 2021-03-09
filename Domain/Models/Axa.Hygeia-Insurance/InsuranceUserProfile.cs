using Domain.Models.Axa.Hygeia_Insurance;
using Domain.Models.ReportAndLogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.Models
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
        /// <summary>
        /// If profile is registered under a company or group. Value can either be : "active","inactive","pending"
        /// </summary>
        public string CompanySubscribedStatus { get; set; }
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
        /// Represent user status in an insurance cycle. Value limited to "active","inactive",null
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
        /// Insurance user subscription status. Value limited to "active","inactive",null
        /// </summary>
        public bool? SubscriptionStatus { get; set; }
        /// <summary>
        /// Insurance Provider user was enrolled into. Value is limited to "hygeia","axamansard"
        /// </summary>
        public string InsuranceService { get; set; }
        /// <summary>
        /// Date insurance profile was created
        /// </summary>
        public DateTime? DateCreated { get; set; }
        public List<DebitCard> Cards { get; set; }
        public List<PaymentReference> PaymentReferences { get; set; }
        public List<ActivityLog> HealthInsuredActivityLogs { get; set; }
    }
}
