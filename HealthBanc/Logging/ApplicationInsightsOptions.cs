using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Logging
{
    public class ApplicationInsightsOptions
    {
        public bool Enabled { get; set; }
        public string InstrumentalKey { get; set; }
    }
}
