using Application.Common.DTO;
using Application.Service;
using Application.Service.DTO;
using Application.Service.Queries;
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
        public async Task<IActionResult> List([FromQuery] GetServiceListQuery query)
        {
            return HandleResult(await Mediator.Send(query));
        }
    }
}
