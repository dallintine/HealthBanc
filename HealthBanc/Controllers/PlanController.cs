using Application.CommonDTO;
using Application.Plans;
using Infrastructure.Helpers;
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
        public async Task<IActionResult> UserPlanList([FromQuery]List.Query queryList)
        {
            return HandleResult(await Mediator.Send(queryList));
        }

        /// <summary>
        /// Create Plan
        /// </summary>
        /// <param name="createCommand"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        public async Task<IActionResult> Create(Create.Command createCommand)
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
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(404, Type = typeof(BaseResponse))]
        public async Task<IActionResult> Edit(Edit.Command editCommand)
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
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(404, Type = typeof(BaseResponse))]
        public async Task<IActionResult> Delete([FromQuery] Delete.Command deleteCommand)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(deleteCommand));
        }
    }
}
