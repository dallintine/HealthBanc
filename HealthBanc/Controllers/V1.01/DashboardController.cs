using Application.Common.DTO;
using Application.Common.Helpers;
using Application.Dashboard.DTO;
using Application.Dashboard.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthBanc.Controllers.V1._01
{
    [ApiVersion("1.01")]
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
        [Authorize(Roles = "Admin")]
        [Cached(500)]
        [ProducesResponseType(200, Type = typeof(BaseResponse<DashboardSummaryQuery>))]
        public async Task<IActionResult> Summary([FromQuery] GetDashboardSummaryQuery query)
        {
            return HandleResult(await Mediator.Send(query));
        }
    }
}
