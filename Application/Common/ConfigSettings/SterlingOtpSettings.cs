using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.ConfigSettings
{
    public class SterlingOtpSettings
    {
        public string SecretKey { get; set; }
        public string Url { get; set; }
        public string SetUpKey { get; set; }
        public string QRCodeURL { get; set; }
        public string PairURL { get; set; }
        public string ValidateURL { get; set; }
    }
}