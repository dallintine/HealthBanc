using Application.Common.DTO;
using Application.Dashboard.DTO;
using Application.Dashboard.Queries;
using Microsoft.AspNetCore.Mvc;

namespace HealthBanc.Controllers
{
    public class DashboardController : BaseApiController
    {
        public DashboardController()
        {
        }

        /// <summary>
        /// Dashboard Summary
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        [ProducesResponseType(200, Type = typeof(BaseResponse<DashboardSummaryQuery>))]
        public async Task<IActionResult> Summary([FromQuery] GetDashboardSummaryQuery query)
        {
            return HandleResult(await Mediator.Send(query));
        }
    }
}
