using Application.Common.DTO;
using Application.Common.Helpers;
using Application.Subscriptions.DTO;
using Application.Subscriptions.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthBanc.Controllers.V1._01
{
    [ApiVersion("1.01")]
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
        [Authorize(Roles = "Admin")]
        [Cached(500)]
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
        [Authorize(Roles = "Admin")]
        [Cached(200)]
        [ProducesResponseType(200, Type = typeof(BaseResponse<PaymentSummaryDTO>))]
        public async Task<IActionResult> PaymentSummary([FromQuery] GetPaymentSummaryQuery query)
        {
            return HandleResult(await Mediator.Send(query));
        }

        /// <summary>
        /// Export List
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        public async Task<IActionResult> ExportList([FromQuery] ExportPaymentList query)
        {
            var response = await Mediator.Send(query);
            string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            string fileName = "PaymentList.xlsx";
            var content = response.Data as byte[];
            return File(content, contentType, fileName);
        }
    }
}
