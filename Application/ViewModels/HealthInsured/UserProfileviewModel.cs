using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Application.ViewModels.HealthInsured
{
    public class UserProfileviewModel
    {
        [Required]
        public string Gender { get; set; }
        public string CustomerNo { get; set; }
        public string MaidenName { get; set; }
        [Required]
        public DateTime DateOfBirth { get; set; }
        [Required]
        public string ContactAddress { get; set; }
        [Required]
        public string Occupation { get; set; }
        [Required]
        public string MaritalStatus { get; set; }
        [Required]
        public string CareProviderName { get; set; }
        public string CPPhone { get; set; }
        public string CPAddress { get; set; }
        public string CPCity { get; set; }
        public string AlternateHospital { get; set; }
        [Required]
        public string PlanCode { get; set; }
        [Required]
        public string StateOfResidence { get; set; }
        [Required]
        public string TownOfResidence { get; set; }
        public string InsuranceService { get; set; }
    }
}
