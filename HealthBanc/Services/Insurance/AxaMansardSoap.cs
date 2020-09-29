using AxaMansardSoap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace HealthBanc.Services.Insurance
{
    public class AxaMansardSoap : IAxaMansardSoap
    {
        public readonly string serviceUrl = "https://careers.axamansard.com/esales/webservice/axamdemo.asmx";
        public readonly EndpointAddress endpointAddress;
        public readonly BasicHttpBinding basicHttpBinding;
        public AxaMansardSoap()
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
        }
        public async Task<axamdemoSoapClient> GetInstanceAsync()
        {
            return await Task.Run(() => new axamdemoSoapClient(basicHttpBinding, endpointAddress));
        }

        public async Task<getTokenResponse> GetTokenRequest()
        {
            var client = await GetInstanceAsync();
            var response = await client.getTokenAsync("STBANK01", "STBank@01-234");
            return response;
        }

        public async Task<GetHealthProvidersResponse> GetHealthProvider(string healthPlan, string state, string token)
        {
            var client = await GetInstanceAsync();
            var response = await client.GetHealthProvidersAsync(healthPlan, state, token);
            return response;
        }

        public async Task<GetTownsResponse> GetTown(string state, string token)
        {
            var client = await GetInstanceAsync();
            var response = await client.GetTownsAsync(state, token);
            return response;
        }

        public async Task<SaveHealthResponse> SaveHealth(iHealth health, string token)
        {
            var client = await GetInstanceAsync();
            var response = await client.SaveHealthAsync(health, token);
            return response;
        }
    }
}
