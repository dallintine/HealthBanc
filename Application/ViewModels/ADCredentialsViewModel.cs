using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Application.ViewModels
{
    public class ADCredentialsViewModel
    {
        [Required]
        public string AD_Username { get; set; }

        [Required]
        public string AD_Password { get; set; }

        [Required]
        public string AD_OTP { get; set; }
    }
}
