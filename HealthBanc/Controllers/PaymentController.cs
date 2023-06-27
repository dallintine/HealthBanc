using Application.Common.DTO;
using Application.Payment;
using Infrastructure.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace HealthBanc.Controllers
{
    public class PaymentController : BaseApiController
    {
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(ILogger<PaymentController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Register User
        /// </summary>
        /// <param name="initializeCommand"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "User")]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(404, Type = typeof(BaseResponse))]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        public async Task<IActionResult> Initialize(Initialize.Command initializeCommand)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(initializeCommand));
        }

        /// <summary>
        /// Paystack Payment Callback
        /// </summary>
        /// <param name="callbackCommand"></param>
        /// <returns></returns>
        [HttpPatch("[action]")]
        [Authorize(Roles = "User")]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(404, Type = typeof(BaseResponse))]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        public async Task<IActionResult> CallBack(CallBack.Command callbackCommand)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(callbackCommand));
        }
    }
}
