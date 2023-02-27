using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Application.ViewModels.UserReg_Login
{
    public class GetOTPViewModel
    {
        [Required]
        public string Email { get; set; }
        public string App { get; set; }
    }
}
