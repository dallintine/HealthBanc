using HealthBanc.Helpers;
using Microsoft.Extensions.Options;
using ServiceReference1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace HealthBanc.Services._2FAService
{
    public class OTPValidationSoap: IOTPValidationSoap
    {
        public readonly string serviceUrl = "https://az-cpibap2-serv/OTPCentralService.asmx";
        public readonly EndpointAddress endpointAddress;
        public readonly BasicHttpBinding basicHttpBinding;
        private OtpValidations Options { get; }
        public OTPValidationSoap()
        {

        }
        public OTPValidationSoap(IOptions<OtpValidations> optionAccessor)
        {
            Options = optionAccessor.Value;
            endpointAddress = new EndpointAddress(serviceUrl);

            basicHttpBinding =
                new BasicHttpBinding(endpointAddress.Uri.Scheme.ToLower() == "http" ?
                            BasicHttpSecurityMode.None : BasicHttpSecurityMode.Transport);

            //Please set the time accordingly, this is only for demo
            basicHttpBinding.OpenTimeout = TimeSpan.MaxValue;
            basicHttpBinding.CloseTimeout = TimeSpan.MaxValue;
            basicHttpBinding.ReceiveTimeout = TimeSpan.MaxValue;
            basicHttpBinding.SendTimeout = TimeSpan.MaxValue;
        }     
    }
}
