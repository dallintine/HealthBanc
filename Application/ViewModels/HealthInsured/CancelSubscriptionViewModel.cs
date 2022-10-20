using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Application.ViewModels.HealthInsured
{
    public class CancelSubscriptionViewModel
    {
        [Required]
        public string Reason { get; set; }
    }
}
