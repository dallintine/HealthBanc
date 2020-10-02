using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.DTO
{
    public class InactiveUsersDTO
    {
        public string TransId { get; set; }
        public string Gender { get; set; }
        public string Surname { get; set; }
        public string Othernames { get; set; }
        public string MaidenName { get; set; }
        public string DateOfBirth { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string ContactAddress { get; set; }
        public string CareProviderName { get; set; }
        public string CPAddress { get; set; }
        public string CPCity { get; set; }
        public string AlternateHospital { get; set; }
        public string PlanCode { get; set; }
        public string Premium { get; set; }
        public string StateOfResidence { get; set; }
        public string TownOfResidence { get; set; }
        public bool SubscriptionStatus { get; set; }
    }    
}
