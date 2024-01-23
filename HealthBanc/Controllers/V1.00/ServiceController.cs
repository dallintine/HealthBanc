using Application.Common.DTO;
using Application.Common.Helpers;
using Application.ProspectivePartner.Command;
using Application.Service;
using Application.Service.Commands;
using Application.Service.DTO;
using Application.Service.Queries;
using Infrastructure.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthBanc.Controllers.V1._00
{

    [ApiVersion("1.00")]
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
        [Cached(500)]
        [ProducesResponseType(200, Type = typeof(BaseResponse<List<ServiceDTO>>))]
        public async Task<IActionResult> List([FromQuery] GetServiceListQuery query)
        {
            return HandleResult(await Mediator.Send(query));
        }

        /// <summary>
        /// Create Service
        /// </summary>
        /// <param name="createCommand"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        public async Task<IActionResult> Create(CreateServiceCommand createCommand)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(createCommand));
        }

        /// <summary>
        /// Become a Partner
        /// </summary>
        /// <param name="createCommand"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        public async Task<IActionResult> CreateProspectivePartnerCommand(CreateProspectivePartnerCommand createCommand)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(createCommand));
        }

        /// <summary>
        /// Edit Service
        /// </summary>
        /// <param name="editCommand"></param>
        /// <returns></returns>
        [HttpPatch("[action]")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(404, Type = typeof(BaseResponse))]
        public async Task<IActionResult> Edit(UpdateServiceCommand editCommand)
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
        public async Task<IActionResult> Delete([FromQuery] DeleteServiceCommand deleteCommand)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(deleteCommand));
        }
    }
}
