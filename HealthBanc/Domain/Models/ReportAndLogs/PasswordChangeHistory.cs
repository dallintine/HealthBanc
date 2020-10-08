using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Domain.Models.ReportAndLogs
{
    public class PasswordChangeHistory
    {
        public Guid Id { get; set; }
        public int ApplicationUserid { get; set; }
        public string Email { get; set; }
        public bool ChangePassword { get; set; }
        public bool ResetPassword { get; set; }
        public DateTime Date { get; set; }
    }
}
