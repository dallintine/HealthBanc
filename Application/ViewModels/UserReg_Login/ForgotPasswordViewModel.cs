using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Application.ViewModels.UserReg_Login
{
    public class ForgotPasswordViewModel
    {
        [Required,DataType(DataType.EmailAddress)]
        public string Username { get; set; }
    }
}
