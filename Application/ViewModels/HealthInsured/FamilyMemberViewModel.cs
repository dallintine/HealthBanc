using System;
using System.Collections.Generic;
using System.Text;

namespace Application.ViewModels.HealthInsured
{
    public class FamilyMemberViewModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Gender { get; set; }
        public string MaidenName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Occupation { get; set; }
        public string MaritalStatus { get; set; }
        public string CareProviderName { get; set; }
        public string CPAddress { get; set; }
        public string CPCity { get; set; }
        public string StateOfResidence { get; set; }
        public string TownOfResidence { get; set; }
        public string ContactAddress { get; set; }
        public string PlanCode { get; set; }
        public Decimal Premium { get; set; }
    }
}
