using Application.Common.DTO;
using Application.Plans;
using Application.Plans.Commands;
using Application.Plans.DTO;
using Application.Plans.Queries;
using Infrastructure.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthBanc.Controllers
{
    public class PlanController : BaseApiController
    {
        private readonly ILogger<PlanController> _logger;

        public PlanController(ILogger<PlanController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Plan List
        /// </summary>
        /// <param name="queryList"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        [ProducesResponseType(200, Type = typeof(BaseResponse<List<PlanDTO>>))]
        public async Task<IActionResult> UserPlanList([FromQuery] GetPlanListQuery queryList)
        {
            return HandleResult(await Mediator.Send(queryList));
        }

        /// <summary>
        /// Create Plan
        /// </summary>
        /// <param name="createCommand"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        public async Task<IActionResult> Create(CreatePlanCommand createCommand)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(createCommand));
        }

        /// <summary>
        /// Edit Plan
        /// </summary>
        /// <param name="editCommand"></param>
        /// <returns></returns>
        [HttpPatch("[action]")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(404, Type = typeof(BaseResponse))]
        public async Task<IActionResult> Edit(UpdatePlanCommand editCommand)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(editCommand));
        }


        /// <summary>
        /// Edit Plan
        /// </summary>
        /// <param name="deleteCommand"></param>
        /// <returns></returns>
        [HttpDelete("[action]")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(404, Type = typeof(BaseResponse))]
        public async Task<IActionResult> Delete([FromQuery] DeletePlanCommand deleteCommand)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(deleteCommand));
        }
    }
}
