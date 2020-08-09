using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.ViewModels
{
    public class NotificationViewModel
    {
        [Required]
        public string Subject { get; set; }
        [Required]
        public string Message { get; set; }
        [Required]
        public DateTime ScheduleDate { get; set; }
        public IFormFile Image { get; set; }
        [Required]
        public int ServiceId { get; set; }
        [Required]
        public int Status { get; set; }
    }
}
