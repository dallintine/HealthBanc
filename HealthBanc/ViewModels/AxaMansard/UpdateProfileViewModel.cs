using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.ViewModels.AxaMansard
{
    public class UpdateProfileViewModel
    {
        [Required]
        public string MaritalStatus { get; set; }
        [Required]
        public string Occupation { get; set; }
        [Required]
        public string ContactAddress { get; set; }
        [Required]
        public string StateOfResidence { get; set; }
        [Required]
        public string TownOfResidence { get; set; }
        [Required]
        public string CareProviderName { get; set; }
        [Required]
        public string AlternateHospital { get; set; }
        [Required]
        public string PlanCode { get; set; }
    }
}