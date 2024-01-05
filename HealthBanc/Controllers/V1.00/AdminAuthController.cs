using Application.AdminAuth.Commands;
using Application.Common.DTO;
using Application.Identity;
using Application.Identity.DTO;
using Infrastructure.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace HealthBanc.Controllers.V1._00
{

    [ApiVersion("1.00")]
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
        [ProducesResponseType(200, Type = typeof(BaseResponse<LoginResponseDTO>))]
        public async Task<IActionResult> Login(AdminLoginCommand command)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(command));
        }

        /// <summary>
        /// Reset Password
        /// </summary>
        /// <param name="resetPassword"></param>
        /// <returns></returns>
        [HttpPatch("[action]")]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        public async Task<IActionResult> ResetPassword([Required] string email, [Required] string emailToken, [FromBody] ResetPasswordDTO resetPassword)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(new ResetPasswordCommand { Email = email, EmailToken = emailToken, Password = resetPassword.Password }));
        }
    }
}
