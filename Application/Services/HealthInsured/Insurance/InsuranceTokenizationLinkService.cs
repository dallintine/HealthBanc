using Application.Services.HealthInsured_AxaMansard.Insurance;
using Domain.Models.Axa_Hygeia_Insurance;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.HealthInsured.Insurance
{
    public class InsuranceTokenizationLinkService
    {
        private readonly TokenizationService _tokenizationService;

        public InsuranceTokenizationLinkService(TokenizationService tokenizationService )
        {
            _tokenizationService = tokenizationService;
        }

        public async Task Process_SuccessfulReferee_FirstTimePayment(InsuranceUserProfile insuranceUserProfile)
        {
            await _tokenizationService.Process_SuccessfulReferee_FirstTimePayment(insuranceUserProfile);
        }
    }
}
