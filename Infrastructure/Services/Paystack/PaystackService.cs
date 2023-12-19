using Application.Common.ConfigSettings;
using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Infrastructure.Services;
using Application.Payment.DTO;

namespace Infrastructure.Paystack
{
    public class PaystackService : BaseService , IPaystackService
    {
        private readonly ILogger<PaystackService> _logger;
        private readonly IHttpConnection _httpConnection;
        private readonly PaystackSettings _paystackSettings;

        public PaystackService(ILogger<PaystackService> logger, IHttpConnection httpConnection, IOptions<PaystackSettings> paystackSettings ,
            IHttpContextAccessor accessor) 
            : base(accessor)
        {
            _logger = logger;
            _httpConnection = httpConnection;
            _paystackSettings = paystackSettings.Value;
        }

        public async Task<InitializePaymentResponse> InitlilizePayment(InitializePaymentRequest initializePayment)
        {
            initializePayment.Email = Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
            //initializePayment.Callback_url = $"{_appEndpointSettings.ApiBaseUrl}{_paystackSettings.CallbackUrl}";
            var payload = JsonConvert.SerializeObject(initializePayment);
            _logger.LogInformation($"Initilizing Paystack Payment [Payload : {payload} ] \n");
            using var request = new HttpRequestMessage(new HttpMethod("POST"), _paystackSettings.InitializePayment);
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_paystackSettings.SecretKey}");
            HttpContent content = new StringContent(payload, Encoding.UTF8, "application/json");
            return await _httpConnection.DevAPIRequest<InitializePaymentResponse>(request, content, "PaystackClientClient");
        }

        public async Task<VerifyPaymentResponse> VerifyPayment(string reference)
        {
            _logger.LogInformation($"Verifying Paystack Payment [Reference : {reference} ] \n");
            using var request = new HttpRequestMessage(new HttpMethod("GET"), $"{_paystackSettings.VerifyPayment}/{reference}");
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_paystackSettings.SecretKey}");
            return await _httpConnection.DevAPIRequest<VerifyPaymentResponse>(request, null, "PaystackClientClient");
        }
    }
}
