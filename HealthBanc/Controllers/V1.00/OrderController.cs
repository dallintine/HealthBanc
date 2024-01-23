using Application.Common.DTO;
using Application.Orders.DTO;
using Application.Orders.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthBanc.Controllers.V1._00
{

    [ApiVersion("1.00")]
    public class OrderController : BaseApiController
    {
        private readonly ILogger<OrderController> _logger;

        public OrderController(ILogger<OrderController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Subscription List
        /// </summary>
        /// <param name="queryList"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        [Authorize(Roles = "Admin,User")]
        [ProducesResponseType(200, Type = typeof(PageBaseResponse<List<OrderDTO>>))]
        public async Task<IActionResult> List([FromQuery] GetOrdersListQuery queryList)
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
        [ProducesResponseType(200, Type = typeof(BaseResponse<PaymentSummaryDTO>))]
        public async Task<IActionResult> PaymentSummary([FromQuery] GetPaymentSummaryQuery query)
        {
            return HandleResult(await Mediator.Send(query));
        }


        /// <summary>
        /// Customer Purchase AnalyticsQuery
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        [Authorize(Roles = "User")]
        [ProducesResponseType(200, Type = typeof(BaseResponse<CustomerPurchaseAnalyticsDTO>))]
        public async Task<IActionResult> CustomerPurchaseAnalytics([FromQuery] CustomerPurchaseAnalyticsQuery query)
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
