using Application.AdminAuth.Commands;
using Application.Common.DTO;
using Application.Identity;
using Infrastructure.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthBanc.Controllers
{
    public class AdminAuthController : BaseApiController
    {
        private readonly ILogger<IdentityController> _logger;

        public AdminAuthController(ILogger<IdentityController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Create Admin
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        public async Task<IActionResult> Create(CreateAdminCommand command)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(command));
        }

        /// <summary>
        /// Signin Admin
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(404, Type = typeof(BaseResponse))]
        [ProducesResponseType(200, Type = typeof(BaseResponse<LoginResponse>))]
        public async Task<IActionResult> Login(AdminLoginCommand command)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(command));
        }
    }
}
