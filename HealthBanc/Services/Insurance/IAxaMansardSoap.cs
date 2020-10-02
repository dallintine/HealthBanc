using AxaMansardSoap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Services.Insurance
{
    public interface IAxaMansardSoap
    {
        Task<GetHealthPlansResponse> GetHealthPlansAsync(string token);
        Task<GetHealthPremiumResponse> GetHealthPremiumAsync(string token, string planCode);
        Task<GetHealthProvidersResponse> GetHealthProvider(string healthPlan, string state, string token);
        Task<GetIdentificationTypesResponse> GetIdentificaionTypesAsync(string token);
        Task<axamdemoSoapClient> GetInstanceAsync();
        Task<getTokenResponse> GetTokenRequest();
        Task<GetTownsResponse> GetTown(string state, string token);
        Task<SaveHealthResponse> SaveHealth(iHealth health, string token);
    }
}
