using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Application.ViewModels
{
    public class ADCredentialsViewModel
    {
        [Required]
        [StringLength(20, ErrorMessage = "Username cannot be longer than 20 characters.")]
        public string AD_Username { get; set; }

        [Required, DataType(DataType.Password)]
        [StringLength(20, ErrorMessage = "Password cannot be longer than 20 characters.")]
        public string AD_Password { get; set; }

        [Required]
        [StringLength(6, ErrorMessage = "OTP cannot be longer than 6 characters.")]
        public string AD_OTP { get; set; }
    }
}
