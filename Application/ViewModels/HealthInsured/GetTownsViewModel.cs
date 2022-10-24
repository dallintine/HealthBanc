using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Application.ViewModels.HealthInsured
{
    public class GetTownsViewModel
    {
        [Required]
        public string State { get; set; }
        [Required]
        public string InsurancePovider { get; set; }
    }
}
