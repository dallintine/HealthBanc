using ServiceReference1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace HealthBanc
{
    public class test
    {
        public readonly string serviceUrl = "http://10.0.41.189:833/IBSServices.asmx";
        public readonly EndpointAddress endpointAddress;
        public readonly BasicHttpBinding basicHttpBinding;

        public test()
        {

            endpointAddress = new EndpointAddress(serviceUrl);

            basicHttpBinding =
                new BasicHttpBinding(endpointAddress.Uri.Scheme.ToLower() == "http" ?
                            BasicHttpSecurityMode.None : BasicHttpSecurityMode.Transport);

            //Please set the time accordingly, this is only for demo
            basicHttpBinding.OpenTimeout = TimeSpan.MaxValue;
            basicHttpBinding.CloseTimeout = TimeSpan.MaxValue;
            basicHttpBinding.ReceiveTimeout = TimeSpan.MaxValue;
            basicHttpBinding.SendTimeout = TimeSpan.MaxValue;
            basicHttpBinding.UseDefaultWebProxy = true;
        }

        private async Task<BSServicesSoapClient> GetInstanceAsync()
        {
            return await Task.Run(() => new BSServicesSoapClient(basicHttpBinding, endpointAddress));
        }

        //public async Task<IBSBridgeResponse> OtpValidationAsync(string otp, string username)
        //{
        //    var response2 = new IBSBridgeResponse();
        //    try
        //    {
        //        var client = await GetInstanceAsync();
        //        var response = await client.IBSBridgeAsync();
        //        _logger.LogError(response.ToString());
        //        return response;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogCritical("OTP validation exception :" + ex.ToString(), ex.ToString());
        //        _logger.LogError("OTP validation Inner exception :" + ex.InnerException.StackTrace.ToString());
        //    }
        //    return response2;
        //}
    }
}
