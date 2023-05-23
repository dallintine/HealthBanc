using Application.CommonDTO;
using Application.Vendors;
using Infrastructure.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace HealthBanc.Controllers
{
    public class VendorController : BaseApiController
    {
        private readonly ILogger<VendorController> _logger;

        public VendorController(ILogger<VendorController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Vendor List
        /// </summary>
        /// <returns></returns>
        [HttpGet("[action]")]
        [ProducesResponseType(200, Type = typeof(BaseResponse<List<VendorDTO>>))]
        public async Task<IActionResult> List()
        {
            return HandleResult(await Mediator.Send(new List.Query()));
        }

        /// <summary>
        /// Create Product
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
        /// Edit Product
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
        /// Delete Product
        /// </summary>
        /// <param name="deleteCommand"></param>
        /// <returns></returns>
        [HttpDelete("[action]")]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(404, Type = typeof(BaseResponse))]
        public async Task<IActionResult> Delete([FromQuery]Delete.Command deleteCommand)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(deleteCommand));
        }
    }
}
