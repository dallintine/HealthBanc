using Application.Common.DTO;
using Application.Subscriptions.DTO;
using Application.Subscriptions.Queries;
using Microsoft.AspNetCore.Mvc;

namespace HealthBanc.Controllers
{
    public class SubscriptionController : BaseApiController
    {
        private readonly ILogger<SubscriptionController> _logger;

        public SubscriptionController(ILogger<SubscriptionController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Subscription List
        /// </summary>
        /// <param name="queryList"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        [ProducesResponseType(200, Type = typeof(PageBaseResponse<List<SubscriptionDTO>>))]
        public async Task<IActionResult> List([FromQuery] GetSubscriptionListQuery queryList)
        {
            return HandleResult(await Mediator.Send(queryList));
        }


        /// <summary>
        /// Payment Summary
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        [ProducesResponseType(200, Type = typeof(BaseResponse<PaymentSummaryDTO>))]
        public async Task<IActionResult> PaymentSummary([FromQuery] GetPaymentSummaryQuery query)
        {
            return HandleResult(await Mediator.Send(query));
        }
    }
}
