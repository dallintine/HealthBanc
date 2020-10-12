using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.ViewModels
{
    public class AuditLogViewModel
    {
        public AuditLogViewModel()
        {
        }

        public AuditLogViewModel(int applicationUserId, string transactionId, string beforeEventContent, string actionApplied, string afterEventContent)
        {
            ApplicationUserId = applicationUserId;
            TransactionId = transactionId;
            BeforeEventContent = beforeEventContent;
            ActionApplied = actionApplied;
            AfterEventContent = afterEventContent;
        }

        [Required]
        public int ApplicationUserId { get; set; }
        public string TransactionId { get; set; }
        [Required]
        public string BeforeEventContent { get; set; }
        [Required]
        public string ActionApplied { get; set; }
        [Required]
        public string AfterEventContent { get; set; }
    }
}
