using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.Models.ReportAndLogs
{
    public class UserAuditLog
    {
        [Required]
        public Guid Id { get; set; }
        [Required]
        public int ApplicationUserId { get; set; }
        public string TransactionId { get; set; }
        public string BeforeEventContent { get; set; }
        [Required]
        public string ActionApplied { get; set; }
        public string AfterEventContent { get; set; }
        [Required]
        public DateTime Date { get; set; }
        public string IPAddress { get; set; }
        public string Device { get; set; }
        public string MACAddress { get; set; }
    }
}
