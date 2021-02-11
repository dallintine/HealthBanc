using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Application.ViewModels.HealthInsured
{
    public class UploadViewModel
    {
        [Required]
        public IFormFile FileUpload { get; set; }
    }
}
