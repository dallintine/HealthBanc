using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.ViewModels
{
    public class ADCredentials
    {
        public string AD_Username { get; set; }

        public string AD_Password { get; set; }
    }

    public class ADCredentialsRoot
    {
        public ADCredentials AD_Credentials { get; set; }
    }
}
