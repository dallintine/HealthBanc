using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Application.ViewModels.HealthInsured
{
    public class AdminUpdateProfileAsyncViewModel : UpdateProfileViewModel
    {
        [Required,DataType(DataType.EmailAddress)]
        [EmailAddress]
        public string Email { get; set; }   
    }
}
