using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Helpers
{
    public class SterlingOtp
    {
        public SterlingOtpConfig SterlingOtpConfig { get; set; }
    }

    public class SterlingOtpConfig
    {
        public string Hashkey { get; set; }
    }
}
