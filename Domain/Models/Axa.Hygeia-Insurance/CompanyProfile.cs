using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models.Axa.Hygeia_Insurance
{
    public class CompanyProfile
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string PendingJobId { get; set; }
        public string CompanyName { get; set; }
        public string CompanyEmail { get; set; }
        public string Industry { get; set; }
        public string CompanySize { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool ProfileCompleted { get; set; }
        public bool TokenizationCompleted { get; set; }
        public decimal? PresentCyclePremiumFee { get; set; }
        public decimal? NextCyclePremiumFee { get; set; }
        public DateTime NextPaymentDate { get; set; }
        public string OTPCode { get; set; }
        public string OTPJobId { get; set; }
        public string InsuranceService { get; set; }
        public List<InsuranceUserProfile> InsuranceUserProfiles { get; set; }
        public List<CompanyInsuranceUser> CompanyInsuranceUsers { get; set; }
        public List<DebitCard> Cards { get; set; }
    }
}
