using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Application.ViewModels.HealthInsured
{
    public class UpdateFamilyMemberProfileViewModel : FamilyMemberViewModel
    {
        [Required]
        public int FamilyMemberId { get; set; }
    }
}
