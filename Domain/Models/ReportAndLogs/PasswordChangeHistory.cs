using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.Models.ReportAndLogs
{
    public class PasswordChangeHistory
    {
        public PasswordChangeHistory()
        {

        }
        public PasswordChangeHistory(int applicationUserid, string email, bool changePassword, bool resetPassword)
        {
            Id = new Guid();
            ApplicationUserid = applicationUserid;
            Email = email;
            ChangePassword = changePassword;
            ResetPassword = resetPassword;
            Date = DateTime.Now;
        }

        public Guid Id { get; set; }
        public int ApplicationUserid { get; set; }
        public string Email { get; set; }
        public bool ChangePassword { get; set; }
        public bool ResetPassword { get; set; }
        public DateTime Date { get; set; }
    }
}
