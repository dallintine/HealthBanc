using Application.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.DTO.HealthInsured_AxaMansard
{
    public class IndividualProfileDTO
    {
        public int Id { get; set; }
        public string TransId { get; set; }
        public string EnrolleeNumber { get; set; }
        public string Gender { get; set; }
        public string Surname { get; set; }
        public string CompanyName { get; set; }
        public string Othernames { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
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
        public DateTime EndActiveStatusDate { get; set; }
        public DateTime StartActiveStatusDate { get; set; }
        public List<CardDTO> CardDTOs { get; set; }
        public List<TransactionLogDTO> TransactionLogDTOs { get; set; }
    }
}
