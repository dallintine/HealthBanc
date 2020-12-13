using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Application.ViewModels.UserReg_Login
{
    public class ResetPasswordViewModel
    {        
        [Required , DataType(DataType.Password)]
        public string Password { get; set; }
        [Required , DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Password and Confirm Password Do Not Match")]
        public string ConfirmPassword { get; set; }
    }
}
