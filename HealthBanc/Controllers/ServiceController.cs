using Application.CommonDTO;
using Application.Service;
using Microsoft.AspNetCore.Mvc;

namespace HealthBanc.Controllers
{
    public class ServiceController : BaseApiController
    {
        public ServiceController()
        {

        }

        /// <summary>
        /// Service List
        /// </summary>
        /// <returns></returns>
        [HttpGet("[action]")]
        [ProducesResponseType(200, Type = typeof(BaseResponse<List<ServiceDTO>>))]
        public async Task<IActionResult> List()
        {
            return HandleResult(await Mediator.Send(new List.Query()));
        }
    }
}
