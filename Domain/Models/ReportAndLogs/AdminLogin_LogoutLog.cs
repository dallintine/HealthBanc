using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.Models.ReportAndLogs
{
    public class AdminLogin_LogoutLog
    {
        public AdminLogin_LogoutLog()
        {
        }

        public AdminLogin_LogoutLog(int applicationUserid, string email, bool signin, bool signOut, bool loginFailure, bool logOutFailure, bool loginOutHours)
        {
            Id = new Guid();
            ApplicationUserid = applicationUserid;
            Email = email;
            Signin = signin;
            SignOut = signOut;
            LoginFailure = loginFailure;
            LogOutFailure = logOutFailure;
            Date = DateTime.Now;
            LoginOutHours = loginOutHours;
        }

        public Guid Id { get; set; }
        public int ApplicationUserid { get; set; }
        public string Email { get; set; }
        public bool Signin { get; set; }
        public bool SignOut { get; set; }
        public bool LoginFailure { get; set; }
        public bool LogOutFailure { get; set; }
        public DateTime Date { get; set; }
        public  bool? LoginOutHours { get; set; }

    }
}
