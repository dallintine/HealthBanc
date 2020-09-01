using HealthBanc.Request.Tokenize;
using HealthBanc.Response;
using HealthBanc.Response.Tokenize;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HealthBanc.Services.Tokenization
{
    public class TokenizationService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TokenizationService> _logger;

        public TokenizationService(IWebHostEnvironment environment, IHttpClientFactory httpClientFactory,ILogger<TokenizationService> logger)
        {
            _environment = environment;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<ResponseMessage<ChargeCardResponse>> ChargeCard(ChargeCard chargeCard)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("Paystack");
                HttpContent content = new StringContent(JsonConvert.SerializeObject(chargeCard), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync("/api/paystack/ChargeCard", content);
                if (response.IsSuccessStatusCode)
                {
                    var chargeCardResponse = new ChargeCardResponse();
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    chargeCardResponse = JsonConvert.DeserializeObject<ChargeCardResponse>(apiResponse);
                    return new ResponseMessage<ChargeCardResponse>() { Data = chargeCardResponse, Message = "", Status = true };
                }
                return new ResponseMessage<ChargeCardResponse> { Status = false };
            }
            catch(Exception ex)
            {
                _logger.LogCritical("An error occurred while trying to charge cards: " + ex);
                return new ResponseMessage<ChargeCardResponse> { Status = false };
            }
        }
    }
}
