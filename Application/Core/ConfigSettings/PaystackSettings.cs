using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Core.ConfigSettings
{
    public class PaystackSettings
    {
        public string BaseUrl { get; set; }
        public string InitializePayment { get; set; }
        public string VerifyPayment { get; set; }
        public string SecretKey { get; set; }

    }
}