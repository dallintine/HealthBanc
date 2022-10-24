using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Application.ViewModels
{
    public class IdViewModel
    {
        [Required]
        public int? Id { get; set; }
    }
}
