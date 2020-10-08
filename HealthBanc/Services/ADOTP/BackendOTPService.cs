using Microsoft.Extensions.Options;
using SterlingOTP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace HealthBanc.Services.ADOTP
{
    public class BackendOTPService : IBackendOTPService
    {
        public readonly string serviceUrl = "https://az-cpibap2-serv/OTPCentralService.asmx";
        public readonly EndpointAddress endpointAddress;
        public readonly BasicHttpBinding basicHttpBinding;
        public readonly HealthBanc.Helpers.SterlingOtp Options;
        public BackendOTPService(IOptions<HealthBanc.Helpers.SterlingOtp> optionAccessor)
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
        private async Task<OTPCentralServiceSoapClient> GetInstanceAsync()
        {
            return await Task.Run(() => new OTPCentralServiceSoapClient(basicHttpBinding, endpointAddress));
        }

        public async Task<OtpValidationResponse> OtpValidationAsync(string otp, string username)
        {
            var client = await GetInstanceAsync();
            var response = await client.OtpValidationAsync(otp, username, Options.SterlingOtpConfig.Hashkey);
            return response;
        }
    }
}
