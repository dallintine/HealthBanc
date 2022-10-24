using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Application.ViewModels.HealthInsured
{
    public class CreateFamilyProfileViewModel
    {
        [Required]
        public string InsuranceService { get; set; }
    }
}
