using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Application.ViewModels.HealthInsured
{
    public class PayForRefereeViewModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        [Required]
        public string Gender { get; set; }
        public string MaidenName { get; set; }
        [Required]
        public DateTime DateOfBirth { get; set; }
        [Required]
        public string ContactAddress { get; set; }
        public string Occupation { get; set; }
        [Required]
        public string MaritalStatus { get; set; }
        [Required]
        public string CareProviderName { get; set; }
        public string CPPhone { get; set; }
        public string CPAddress { get; set; }
        public string CPCity { get; set; }
        [Required]
        public string PlanCode { get; set; }
        [Required]
        public string StateOfResidence { get; set; }
        [Required]
        public string TownOfResidence { get; set; }
        public string InsuranceService { get; set; }
        public IFormFile UserImage { get; set; }
    }
}
