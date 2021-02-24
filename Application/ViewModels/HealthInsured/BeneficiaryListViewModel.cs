using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Application.ViewModels.HealthInsured
{
    public class BeneficiaryListViewModel
    {
        [Required]
        public List<string> Emails { get; set; }
    }
}
