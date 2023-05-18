using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.CommonDTO
{
    public class UploadImageDTO
    {
        [Required]
        public string Base64Image { get; set; }
        [Required, StringLength(100, ErrorMessage = "FileName cannot be empty or less than 3", MinimumLength = 1)]
        public string FileName { get; set; }
    }

}
