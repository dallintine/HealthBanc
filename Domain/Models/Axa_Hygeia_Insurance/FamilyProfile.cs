using Domain.Models.ReportAndLogs;
using Domain.Models.Wallet;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models.Axa_Hygeia_Insurance
{
    public class FamilyProfile
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

        /// <summary>
        /// Represent a scheduled Job/BackgroundService Id for scheduled email Task
        /// </summary>
        public string PendingEmailJobId { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public bool EmailConfirmed { get; set; }
        /// <summary>
        /// Boolean value to indicate if company first card tokenization has been completed
        /// </summary>
        public bool TokenizationCompleted { get; set; }
        public bool ProfileCompleted { get; set; }

        /// <summary>
        /// The Insurance Service provider. For Possible Providers <see cref="InsuranceProvider"/>
        /// </summary>
        public string InsuranceService { get; set; }
        public DateTime DateCreated { get; set; }
        public List<InsuranceUserProfile> InsuranceUserProfiles { get; set; }
        public List<DebitCard> Cards { get; set; }
        public List<PaymentReference> PaymentReferences { get; set; }
        public List<ActivityLog> HealthInsuredActivityLogs { get; set; }
        public UserWallet UserWallet { get; set; }
    }
}
