using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required, DataType(DataType.Password)]
        public string Password { get; set; }
        [Required, DataType(DataType.Password)]
        public string NewPassword { get; set; }
        [Required, DataType(DataType.Password), Compare("NewPassword", ErrorMessage = "NewPassword and Confirm Password Do Not Match")]
        public string ConfirmPassword { get; set; }
    }
}
