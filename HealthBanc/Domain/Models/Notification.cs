using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HealthBanc.Domain.Models
{
    public class Notification
    {
        public int Id { get; set; }
        [Required]
        public string Subject { get; set; }
        [Required]
        public string Message { get; set; }
        [Required]
        public DateTime ScheduleDate { get; set; }
        public string ImageURl { get; set; }
        [Required]
        public int ServiceId { get; set; }
        public Service Service { get; set; }
        [Required]
        public int Status { get; set; }
    }
}
