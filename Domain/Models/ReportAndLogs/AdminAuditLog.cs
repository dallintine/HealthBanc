using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.Models.ReportAndLogs
{
    public class AdminAuditLog
    {
        [Required]
        public Guid Id { get; set; }
        [Required]
        public int ApplicationUserId { get; set; }
        [Required]
        public int BackendAdminUserId { get; set; }
        [Required]
        public string ActionApplied { get; set; }
        [Required]
        public DateTime Date { get; set; }
        public string IPAddress { get; set; }
        public string Channel { get; set; }
        public BackendAdminUser BackendAdminUser { get; set; }
    }
}
