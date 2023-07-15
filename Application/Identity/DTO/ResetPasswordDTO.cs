using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Identity.DTO
{
    public class ResetPasswordDTO
    {
        [Required]
        public string Password { get; set; }
    }
}
