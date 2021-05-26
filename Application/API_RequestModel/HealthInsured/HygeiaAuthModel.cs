using System;
using System.Collections.Generic;
using System.Text;

namespace Application.API_RequestModel.HealthInsured
{
    public class HygeiaAuthModel
    {
        public string username { get; set; }

        public string password { get; set; }
        public string grant_type { get; set; }
    }
}
