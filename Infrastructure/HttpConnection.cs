using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure
{
    public class HttpConnection : IHttpConnection
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<HttpConnection> _logger;
        public HttpConnection(IHttpClientFactory httpClientFactory, ILogger<HttpConnection> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<T> DevAPIRequest<T>(HttpRequestMessage request, HttpContent content, string client) where T : new()
        {
            var httpClient = _httpClientFactory.CreateClient(client);
            request.Content = content;
            var response = await httpClient.SendAsync(request);
            string apiResponse = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"API Execute Response : {apiResponse}");
            try
            {
                var deserialisedResponse = JsonConvert.DeserializeObject<T>(apiResponse);
                return deserialisedResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred while deserilizing response : {ex}");
                const string message = "Error retrieving response.  Check inner details for more info.";
                var exception = new Exception(message, ex);
                throw exception;
            }
        }
    }
}
