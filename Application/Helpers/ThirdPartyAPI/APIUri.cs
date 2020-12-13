using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Helpers.ThirdPartyAPI
{
    public class AppEndpoint
    {
        public APIUri APIUri { get; set; }        
    }
    public class APIUri
    {
        public string HealthBancSignIn { get; set; }
        public string HealthBancResendEmail { get; set; }
        public string HealthBancForgotPassword { get; set; }
        public string HealthBancApiBase { get; set; }
        public string FiorianoBaseAddress { get; set; }
        public string FiorianoADAuthentication { get; set; }
    }
}
