using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Domain.Models
{
    public class AxaMansardUserProfile
    {
        public int Id { get; set; }
        public string TransId { get; set; }
        public string Gender { get; set; }
        public string CustomerNo { get; set; }
        public string Surname { get; set; }
        public string Othernames { get; set; }
        public string MaidenName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string ContactAddress { get; set; }
        public string Occupation { get; set; }
        public string MaritalStatus { get; set; }
        public string BankName { get; set; }
        public string AccountNo { get; set; }
        public string BVN { get; set; }
        public Decimal? Weight { get; set; }
        public Decimal? Height { get; set; }
        public string BloodGroup { get; set; }
        public string Genotype { get; set; }
        public string Identification { get; set; }
        public int? Religion { get; set; }
        public string Hobbies { get; set; }
        public string CareProviderName { get; set; }
        public string CPPhone { get; set; }
        public string CPAddress { get; set; }
        public string CPCity { get; set; }
        public string CPEmail { get; set; }
        public string AlternateHospital { get; set; }
        public string MedicalCondition { get; set; }
        public string PlanCode { get; set; }
        public Decimal Premium { get; set; }
        public string CustomerPhoto { get; set; }
        public string IdentityPhoto { get; set; }
        public string StateOfResidence { get; set; }
        public string TownOfResidence { get; set; }
    }
}
