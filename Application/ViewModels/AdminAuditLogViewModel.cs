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
        public AdminAuditLogViewModel(int applicationUserId, int backendAdminUserId,string actionApplied,string channel)
        {
            ApplicationUserId = applicationUserId;
            BackendAdminUserId = backendAdminUserId;
            ActionApplied = actionApplied;
            Channel = channel;
        }

        [Required]
        public int ApplicationUserId { get; set; }
        [Required]
        public int BackendAdminUserId { get; set; }
        [Required]
        public string ActionApplied { get; set; }
        [Required]
        public string Channel { get; set; }
    }
}
