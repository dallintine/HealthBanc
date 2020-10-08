using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Domain.Models.ReportAndLogs
{
    public class UserLogin_LogoutLog
    {
        public Guid Id { get; set; }
        public int ApplicationUserid { get; set; }
        public string Email { get; set; }
        public bool Signin { get; set; }
        public bool SignOut { get; set; }
        public bool FailedSigninAttempt { get; set; }
        public DateTime Date { get; set; }
    }
}
