using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Application.ViewModels.UserReg_Login
{
    public class RegistrationViewModel
    {
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required,DataType(DataType.EmailAddress)]
        public string EmailAddress { get; set; }
        [Required,DataType(DataType.PhoneNumber)]
        public string PhoneNumber { get; set; }
        [Required, DataType(DataType.Password)]
        public string Password { get; set; }
        [Required, DataType(DataType.Password), Compare("Password", ErrorMessage = "Password and Confirm Password Do Not Match")]
        public string ConfirmPassword { get; set; }
    }
}
