using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Application.ViewModels
{
    public class ChangeAdminRoleViewModel
    {
        [Required]
        public int RoleId { get; set; }
        [Required, DataType(DataType.EmailAddress)]
        [EmailAddress]
        [StringLength(1000, ErrorMessage = "Invalid Email", MinimumLength = 1)]
        public string Email { get; set; }
    }
}
