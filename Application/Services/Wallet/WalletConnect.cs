using Application.API_RequestModel.Wallet;
using Application.DTO;
using Application.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Wallet
{
    public class WalletConnect
    {
        private readonly ILogger<WalletConnect> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly WalletSettings _walletSettings;

        public WalletConnect(ILogger<WalletConnect> logger,IHttpClientFactory httpClientFactory,IOptions<WalletSettings> WalletSettings)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _walletSettings = WalletSettings.Value;
        }

        private async Task<string> Execute(HttpRequestMessage request, HttpContent content)
        {
            var httpClient = _httpClientFactory.CreateClient("WalletClient");

            request.Content = content;
            var response = await httpClient.SendAsync(request);
            string apiResponse = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"Wallet Client API Execute Response : {apiResponse} \n");
            return apiResponse;
        }

        public async Task<string> CreateWallet(EncryptedModel encryptedRequest)
        {
            var payload = JsonConvert.SerializeObject(encryptedRequest);
            _logger.LogInformation($"Create Wallet Sterling Request [Payload : {payload} ] \n");
            using var request = new HttpRequestMessage(new HttpMethod("POST"), _walletSettings.CreateWallet);
            HttpContent content = new StringContent(payload, Encoding.UTF8, "application/json");
            return await Execute(request, content);
        }

        public async Task<string> WalletDetails(EncryptedModel encryptedRequest)
        {
            var payload = JsonConvert.SerializeObject(encryptedRequest);
            _logger.LogInformation($"Wallet Details Sterling Request [Payload : {payload}] \n");
            using var request = new HttpRequestMessage(new HttpMethod("POST"), _walletSettings.WalletDetails);
            HttpContent content = new StringContent(payload, Encoding.UTF8, "application/json");
            return await Execute(request, content);
        }

        public async Task<string> WalletToSterlingFT(EncryptedModel encryptedRequest)
        {
            var payload = JsonConvert.SerializeObject(encryptedRequest);
            _logger.LogInformation($"Wallet Details Sterling Request  [Payload : {payload}] \n");
            using var request = new HttpRequestMessage(new HttpMethod("POST"), _walletSettings.WalletToSterling);
            HttpContent content = new StringContent(payload, Encoding.UTF8, "application/json");
            return await Execute(request, content);
        }
    }
}
