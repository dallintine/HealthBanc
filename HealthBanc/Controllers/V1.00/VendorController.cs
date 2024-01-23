using Application.Common.DTO;
using Application.Common.Helpers;
using Application.Vendors.Commands;
using Application.Vendors.DTO;
using Application.Vendors.Queries;
using Infrastructure.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthBanc.Controllers.V1._00
{

    [ApiVersion("1.00")]
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
        [Cached(500)]
        [ProducesResponseType(200, Type = typeof(BaseResponse<List<VendorDTO>>))]
        public async Task<IActionResult> List()
        {
            return HandleResult(await Mediator.Send(new GetVendorListQuery()));
        }

        /// <summary>
        /// Get Vendor
        /// </summary>
        /// <returns></returns>
        [HttpGet("[action]")]
        [ProducesResponseType(200, Type = typeof(BaseResponse<VendorDTO>))]
        public async Task<IActionResult> Get([FromQuery] GetVendorQuery request)
        {
            return HandleResult(await Mediator.Send(request));
        }

        /// <summary>
        /// Vendor Purchase History
        /// </summary>
        /// <returns></returns>
        [HttpGet("[action]")]
        [ProducesResponseType(200, Type = typeof(BaseResponse<VendorPurchaseHistoryDTO>))]
        public async Task<IActionResult> PurchaseHistory([FromQuery] GetVendorPurchaseHistory request )
        {
            return HandleResult(await Mediator.Send(request));
        }

        /// <summary>
        /// Create Product
        /// </summary>
        /// <param name="createCommand"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        public async Task<IActionResult> Create(CreateVendorCommand createCommand)
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
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(404, Type = typeof(BaseResponse))]
        public async Task<IActionResult> Edit(UpdateVendorCommand editCommand)
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
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(404, Type = typeof(BaseResponse))]
        public async Task<IActionResult> Delete([FromQuery]DeleteVendorCommand deleteCommand)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(deleteCommand));
        }
    }
}
