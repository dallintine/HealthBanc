using Domain.Models.Axa.Hygeia_Insurance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class InsuranceUserProfile
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? CompanyProfileId { get; set; }
        public string CompanySubscribedStatus { get; set; }
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
        public string Identification { get; set; }
        public string CareProviderName { get; set; }
        public string CPAddress { get; set; }
        public string CPCity { get; set; }
        public string AlternateHospital { get; set; }
        public string AlternateHospitalAddress { get; set; }
        public string PlanCode { get; set; }
        public Decimal Premium { get; set; }
        public string CustomerPhoto { get; set; }
        public string IdentityPhoto { get; set; }
        public string StateOfResidence { get; set; }
        public string TownOfResidence { get; set; }
        public bool? ActiveStatus { get; set; }
        public DateTime EndActiveStatusDate { get; set; }
        public DateTime StartActiveStatusDate { get; set; }
        public bool? SubscriptionStatus { get; set; }
        public string InsuranceService { get; set; }
        public DateTime? DateCreated { get; set; }
        public List<DebitCard> Cards { get; set; }
        public List<PaymentReference> PaymentReferences { get; set; }
        public List<HealthInsuredActivityLog> HealthInsuredActivityLogs { get; set; }
    }
}
