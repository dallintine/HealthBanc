using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Application.ViewModels.HealthInsured
{
    public class RemoveFamilyMemberViewModel
    {
        [Required]
        public int FamiyMemeberId { get; set; }
    }
}
