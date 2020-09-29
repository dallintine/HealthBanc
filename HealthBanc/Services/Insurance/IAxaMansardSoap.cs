using AxaMansardSoap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Services.Insurance
{
    public interface IAxaMansardSoap
    {
        Task<GetHealthProvidersResponse> GetHealthProvider(string healthPlan, string state, string token);
        Task<axamdemoSoapClient> GetInstanceAsync();
        Task<getTokenResponse> GetTokenRequest();
        Task<GetTownsResponse> GetTown(string state, string token);
        Task<SaveHealthResponse> SaveHealth(iHealth health, string token);
    }
}
