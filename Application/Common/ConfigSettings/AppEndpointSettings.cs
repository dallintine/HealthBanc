using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.ConfigSettings
{
    public class AppEndpointSettings
    {
        public string FrontendBaseUrl { get; set; }
        public string ResetPassword { get; set; }
        public string ApiBaseUrl { get; set; }
        public string Login { get; set; }
        public string Environment { get; set; }
        public string FiorianoBaseAddress { get; set; }
        public string FiorianoADAuthentication { get; set; }
        public string Salt { get; set; }
        public string VectorKey { get; set; }
    }
}