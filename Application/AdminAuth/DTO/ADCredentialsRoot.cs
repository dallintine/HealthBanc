using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.AdminAuth.DTO
{
    public class ADCredentialsRoot
    {
        public ADCredentials AD_Credentials { get; set; }
    }
    public class ADCredentials
    {
        public string AD_Username { get; set; }

        public string AD_Password { get; set; }
    }
}
