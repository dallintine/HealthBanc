using Application.CommonDTO;
using Application.Identity;
using Infrastructure.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

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
        /// <param name="registerUserCommand"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        public async Task<IActionResult> RegisterUser(Register.Command registerUserCommand)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(registerUserCommand));
        }

        /// <summary>
        /// Signin User
        /// </summary>
        /// <param name="loginQuery"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(404, Type = typeof(BaseResponse))]
        [ProducesResponseType(200, Type = typeof(BaseResponse<LoginResponse>))]
        public async Task<IActionResult> Login(Login.Query loginQuery)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(loginQuery));
        }

        /// <summary>
        /// SignOut User
        /// </summary>
        /// <returns></returns>
        [HttpPost("[action]")]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(404, Type = typeof(BaseResponse))]
        [ProducesResponseType(200, Type = typeof(BaseResponse<LoginResponse>))]
        public async Task<IActionResult> Logout()
        {
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(new Logout.Query()));
        }

        /// <summary>
        /// Change Password
        /// </summary>
        /// <param name="changePasswordCommand"></param>
        /// <returns></returns>
        [HttpPatch("[action]")]
        [Authorize]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        public async Task<IActionResult> ChangePassword(ChangePassword.Command changePasswordCommand)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(changePasswordCommand));
        }

        /// <summary>
        /// Reset Password
        /// </summary>
        /// <param name="resetPassword"></param>
        /// <returns></returns>
        [HttpPatch("[action]")]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        public async Task<IActionResult> ResetPassword([Required] string email, [Required] string emailToken , ResetPasswordRequest  resetPassword)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(new ResetPassword.Command { Email = email, EmailToken = emailToken, Password = resetPassword.Password}));
        }

        /// <summary>
        /// Reset Password
        /// </summary>
        /// <param name="forgotPasswordQuery"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        public async Task<IActionResult> ForgetPassword(ForgotPassword.Query forgotPasswordQuery)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(forgotPasswordQuery));
        }

        /// <summary>
        /// Confirm Account
        /// </summary>
        /// <param name="confirmAccountCommand"></param>
        /// <returns></returns>
        [HttpPatch("[action]")]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(404, Type = typeof(BaseResponse))]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        public async Task<IActionResult> ConfirmAccount(ConfirmEmail.Command confirmAccountCommand)
        {
            _logger.LogInformation($"Confirm Account Request \n");
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(confirmAccountCommand));
        }

        /// <summary>
        /// Refresh Token
        /// </summary>
        /// <param name="refreshTokenQuery"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(404, Type = typeof(BaseResponse))]
        [ProducesResponseType(200, Type = typeof(BaseResponse<LoginResponse>))]
        public async Task<IActionResult> RefreshToken(RefreshToken.Query refreshTokenQuery)
        {
            _logger.LogInformation($"Refresh Token Request \n");
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(refreshTokenQuery));
        }
    }
}
