using Application.Common.DTO;
using Application.Common.Helpers;
using Application.Customer.DTO;
using Application.Customer.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthBanc.Controllers.V1._00
{

    [ApiVersion("1.00")]
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
        [Cached(500)]
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
            var response = await Mediator.Send(query);
            string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            string fileName = "CustomerList.xlsx";
            var content = response.Data as byte[];
            return File(content, contentType, fileName);
        }
    }
}
