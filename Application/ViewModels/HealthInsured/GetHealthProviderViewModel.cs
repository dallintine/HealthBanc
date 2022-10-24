using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Application.ViewModels.HealthInsured
{
    public class GetHealthProviderViewModel
    {
        [Required]
        public string State { get; set; }
        [Required]
        public string InsurancePovider { get; set; }
        public string City { get; set; }
    }
}
