using System;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.Logging;

namespace Application.Services.HealthInsured_AxaMansard.Insurance
{
    public class TokenizationService
    {
        private readonly ILogger<TokenizationService> _logger;
        public TokenizationService(ILogger<TokenizationService> logger)
        {
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task SchedulePaymentLogic(int userId, int axamansardUserId, PerformContext context)
        {
            await Task.CompletedTask;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task SchedulePaymentLogic(int userId, PerformContext context)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Deactivate all company insurance users
        /// </summary>
        /// <param name="companyUserId"></param>
        /// <returns></returns>
        public async Task DeactivateAllCompanybeneficiaries(int companyUserId)
        {
            await Task.CompletedTask;
        }

        [AutomaticRetry(Attempts = 0)]
        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task ProcessUserActiveStatusCancellation(int userId)
        {
            await Task.CompletedTask;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task ProcessUserActiveStatusCancellation(int insuranceProfileId,PerformContext performContext)
        {            
            await Task.CompletedTask;
        }       
    } 
}

