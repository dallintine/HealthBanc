using Application.Common.DTO;
using Application.Customer.DTO;
using Application.Customer.Queries;
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
        [ProducesResponseType(200, Type = typeof(PageBaseResponse<List<CustomerDTO>>))]
        public async Task<IActionResult> UserPlanList([FromQuery] GetCustomerListQuery queryList)
        {
            return HandleResult(await Mediator.Send(queryList));
        }
    }
}
