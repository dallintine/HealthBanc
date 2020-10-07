using AxaMansardSoap;
using HealthBanc.Helpers;
using Microsoft.Extensions.Options;
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
        private AxaMansard Options { get; }
        public AxaMansardSoap(IOptions<AxaMansard> optionAccessor)
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
        public async Task<axamdemoSoapClient> GetInstanceAsync()
        {
            return await Task.Run(() => new axamdemoSoapClient(basicHttpBinding, endpointAddress));
        }

        public async Task<getTokenResponse> GetTokenRequest()
        {
            var client = await GetInstanceAsync();
            var response = await client.getTokenAsync(Options.AxaMansardConfiguration.Username, Options.AxaMansardConfiguration.Password);
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

        public async Task<GetHealthPlansResponse> GetHealthPlansAsync(string token)
        {
            var client = await GetInstanceAsync();
            var response = await client.GetHealthPlansAsync(token);
            return response;
        }

        public async Task<GetHealthPremiumResponse> GetHealthPremiumAsync(string token,string planCode)
        {
            var client = await GetInstanceAsync();
            var response = await client.GetHealthPremiumAsync(planCode, token);
            return response;
        }

        public async Task<GetIdentificationTypesResponse> GetIdentificaionTypesAsync(string token)
        {
            var client = await GetInstanceAsync();
            var response = await client.GetIdentificationTypesAsync(token);
            return response;
        }
    }
}
