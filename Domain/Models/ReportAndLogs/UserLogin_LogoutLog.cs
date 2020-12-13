using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.Models.ReportAndLogs
{
    public class UserLogin_LogoutLog
    {
        public UserLogin_LogoutLog(int applicationUserid, string email, bool signin, bool signOut, bool failedSigninAttempt)
        {
            Id = new Guid();
            ApplicationUserid = applicationUserid;
            Email = email;
            Signin = signin;
            SignOut = signOut;
            FailedSigninAttempt = failedSigninAttempt;
            Date = DateTime.Now;
        }

        public Guid Id { get; set; }
        public int ApplicationUserid { get; set; }
        public string Email { get; set; }
        public bool Signin { get; set; }
        public bool SignOut { get; set; }
        public bool FailedSigninAttempt { get; set; }
        public DateTime Date { get; set; }
    }
}
