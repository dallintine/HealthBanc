using Application.CommonDTO;
using Application.Plans;
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
        /// <returns></returns>
        [HttpGet("[action]")]
        [ProducesResponseType(200, Type = typeof(BaseResponse<List<PlanDTO>>))]
        public async Task<IActionResult> List()
        {
            return HandleResult(await Mediator.Send(new List.Query()));
        }
    }
}
