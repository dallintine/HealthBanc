using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Domain.Models.ReportAndLogs
{
    public class UserAuditLog
    {
        [Required]
        public Guid Id { get; set; }
        [Required]
        public int ApplicationUserId { get; set; }
        [Required]
        public string TransactionId { get; set; }
        [Required]
        public string BeforeEventContent { get; set; }

        public string ActionApplied { get; set; }
        [Required]
        public string AfterEventContent { get; set; }
        [Required]
        public DateTime Date { get; set; }
        [Required]
        public string IPAddress { get; set; }
        [Required]
        public string Device { get; set; }
        [Required]
        public string MACAddress { get; set; }
    }
}
