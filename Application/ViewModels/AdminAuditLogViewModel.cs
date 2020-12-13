using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Application.ViewModels
{
    public class AdminAuditLogViewModel
    {
        public AdminAuditLogViewModel()
        {

        }
        public AdminAuditLogViewModel(int applicationUserId, int backendAdminUserId, string transactionId, string beforeEventContent, string actionApplied, string afterEventContent)
        {
            ApplicationUserId = applicationUserId;
            BackendAdminUserId = backendAdminUserId;
            TransactionId = transactionId;
            BeforeEventContent = beforeEventContent;
            ActionApplied = actionApplied;
            AfterEventContent = afterEventContent;
        }

        [Required]
        public int ApplicationUserId { get; set; }
        [Required]
        public int BackendAdminUserId { get; set; }
        public string TransactionId { get; set; }
        [Required]
        public string BeforeEventContent { get; set; }
        [Required]
        public string ActionApplied { get; set; }
        [Required]
        public string AfterEventContent { get; set; }
    }
}
