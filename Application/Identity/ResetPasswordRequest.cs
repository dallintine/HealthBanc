using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Identity
{
    public class ResetPasswordRequest
    {
        [Required]
        public string Password { get; set; }
    }
}
