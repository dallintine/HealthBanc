using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTO.HealthInsured_AxaMansard
{
    public class FamilyMembersDTO
    {
        public int Id { get; set; }
        public string TransId { get; set; }
        public string EnrolleeNumber { get; set; }
        public string Gender { get; set; }
        public string Surname { get; set; }
        public string Othernames { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string ContactAddress { get; set; }
        public string Occupation { get; set; }
        public string MaritalStatus { get; set; }
        public string CareProviderName { get; set; }
        public string CPAddress { get; set; }
        public string CPCity { get; set; }
        public string PlanCode { get; set; }
        public Decimal Premium { get; set; }
        public string InsuranceService { get; set; }
        public string StateOfResidence { get; set; }
        public string TownOfResidence { get; set; }
        public bool SubscriptionStatus { get; set; }
        public bool? ActiveStatus { get; set; }
    }
}
