using Application.CommonDTO;
using Application.Core.ConfigSettings;
using Application.Interfaces;
using Application.Payment;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Paystack
{
    public class PaystackService : IPaystackService
    {
        private readonly ILogger<PaystackService> _logger;
        private readonly IHttpConnection _httpConnection;
        private readonly PaystackSettings _paystackSettings;

        public PaystackService(ILogger<PaystackService> logger, IHttpConnection httpConnection, IOptions<PaystackSettings> paystackSettings)
        {
            _logger = logger;
            _httpConnection = httpConnection;
            _paystackSettings = paystackSettings.Value;
        }

        public async Task<BaseResponse> InitlilizePayment(InitializePaystackPayment initializePayment)
        {
            var payload = JsonConvert.SerializeObject(initializePayment);
            _logger.LogInformation($"Initilizing Paystack Payment [Payload : {payload} ] \n");
            using var request = new HttpRequestMessage(new HttpMethod("POST"), _paystackSettings.InitializePayment);
            HttpContent content = new StringContent(payload, Encoding.UTF8, "application/json");
            return await _httpConnection.DevAPIRequest<BaseResponse>(request, content, "DevApiClient");
        }
    }
}
