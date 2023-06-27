using Application.Common.DTO;
using Application.Customer.DTO;
using Application.Customer.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthBanc.Controllers
{
    public class CustomerController : BaseApiController
    {
        public CustomerController()
        {
        }

        /// <summary>
        /// Customer List
        /// </summary>
        /// <param name="queryList"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(200, Type = typeof(PageBaseResponse<List<CustomerDTO>>))]
        public async Task<IActionResult> PaginatedList([FromQuery] GetCustomerListQuery queryList)
        {
            return HandleResult(await Mediator.Send(queryList));
        }

        /// <summary>
        /// Export List
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        public async Task<IActionResult> ExportList([FromQuery] ExportCustomerListQuery query)
        {
            return HandleResult(await Mediator.Send(query));
        }
    }
}
