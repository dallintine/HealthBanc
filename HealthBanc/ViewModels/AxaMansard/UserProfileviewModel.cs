using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.ViewModels.AxaMansard
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
        //[Required]
        public string BankName { get; set; }
        //[Required]
        public string AccountNo { get; set; }
        public string BVN { get; set; }
        public string BloodGroup { get; set; }
        public string Genotype { get; set; }
        [Required]
        public string Identification { get; set; }
        public string Hobbies { get; set; }
        [Required]
        public string CareProviderName { get; set; }
        [Required]
        public string CPPhone { get; set; }
        [Required]
        public string CPAddress { get; set; }
        [Required]
        public string CPCity { get; set; }
        [Required]
        public string CPEmail { get; set; }
        [Required]
        public string AlternateHospital { get; set; }
        public string MedicalCondition { get; set; }
        [Required]
        public string PlanCode { get; set; }
        [Required]
        public IFormFile CustomerPhoto { get; set; }
        [Required]
        public IFormFile IdentityPhoto { get; set; }
        [Required]
        public string StateOfResidence { get; set; }
        [Required]
        public string TownOfResidence { get; set; }
    }
}
