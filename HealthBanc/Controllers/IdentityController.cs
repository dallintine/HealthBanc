using Application.CommonDTO;
using Application.Identity;
using Infrastructure.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace HealthBanc.Controllers
{
    public class IdentityController : BaseApiController
    {
        private readonly ILogger<IdentityController> _logger;

        public IdentityController(ILogger<IdentityController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Register User
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        public async Task<IActionResult> RegisterUser(Register.Command command)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(command));
        }

        /// <summary>
        /// Signin User
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(404, Type = typeof(BaseResponse))]
        [ProducesResponseType(200, Type = typeof(BaseResponse<LoginResponse>))]
        public async Task<IActionResult> Login(Login.Query query)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(query));
        }

        /// <summary>
        /// Change Password
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        [HttpPatch("[action]")]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        public async Task<IActionResult> ChangePassword(ChangePassword.Command command)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(command));
        }

        /// <summary>
        /// Confirm Account
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        [HttpPatch("[action]")]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(404, Type = typeof(BaseResponse))]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        public async Task<IActionResult> ConfirmAccount(ConfirmEmail.Command command)
        {
            _logger.LogInformation($"Confirm Account Request \n");
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(command));
        }

        /// <summary>
        /// Refresh Token
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(404, Type = typeof(BaseResponse))]
        [ProducesResponseType(200, Type = typeof(BaseResponse<LoginResponse>))]
        public async Task<IActionResult> RefreshToken(RefreshToken.Query query)
        {
            _logger.LogInformation($"Refresh Token Request \n");
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(query));
        }
    }
}
