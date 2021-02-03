using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Application.ViewModels.HealthInsured
{
    public class UpdateCorporateUserViewModel
    {
        [Required]
        public string Industry { get; set; }
        [Required]
        public string CompanySize { get; set; }
    }
}
