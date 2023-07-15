using Application.Common.DTO;
using Application.Identity;
using Application.Identity.Commands;
using Application.Identity.DTO;
using Application.Identity.Queries;
using Google.Apis.Auth;
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
        public async Task<IActionResult> RegisterUser(RegisterCommand registerUserCommand)
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
        [ProducesResponseType(200, Type = typeof(BaseResponse<LoginResponseDTO>))]
        public async Task<IActionResult> Login(LoginQuery loginQuery)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(loginQuery));
        }

        /// <summary>
        /// SignOut User
        /// </summary>
        /// <returns></returns>
        [HttpGet("[action]")]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(404, Type = typeof(BaseResponse))]
        [ProducesResponseType(200, Type = typeof(BaseResponse<LoginResponseDTO>))]
        public async Task<IActionResult> Logout()
        {
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(new LogOutQuery()));
        }

        /// <summary>
        /// Change Password
        /// </summary>
        /// <param name="changePasswordCommand"></param>
        /// <returns></returns>
        [HttpPatch("[action]")]
        [Authorize(Roles = "User")]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        public async Task<IActionResult> ChangePassword(ChangePasswordCommand changePasswordCommand)
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
        public async Task<IActionResult> ResetPassword([Required] string email, [Required] string emailToken ,[FromBody] ResetPasswordDTO  resetPassword)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(new ResetPasswordCommand { Email = email, EmailToken = emailToken, Password = resetPassword.Password}));
        }

        /// <summary>
        /// Reset Password
        /// </summary>
        /// <param name="forgotPasswordQuery"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordCommand forgotPasswordCommand)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(forgotPasswordCommand));
        }

        /// <summary>
        /// Resend Confirmation Email
        /// </summary>
        /// <param name="resendConfirmationOTP"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        public async Task<IActionResult> ResendConfirmationEmail(ResendConfirmationOTPQuery resendConfirmationOTP)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(resendConfirmationOTP));
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
        public async Task<IActionResult> ConfirmAccount(ConfirmEmailCommand confirmAccountCommand)
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
        [ProducesResponseType(200, Type = typeof(BaseResponse<LoginResponseDTO>))]
        public async Task<IActionResult> RefreshToken(RefreshTokenQuery refreshTokenQuery)
        {
            _logger.LogInformation($"Refresh Token Request \n");
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(refreshTokenQuery));
        }

        /// <summary>
        /// Goggle Auth Signin/Signup
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(404, Type = typeof(BaseResponse))]
        [ProducesResponseType(200, Type = typeof(BaseResponse<LoginResponseDTO>))]
        public async Task<IActionResult> GoogleAuth(GoggleAuthCommand command)
        {
            _logger.LogInformation($"Goggle Auth Request \n");
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(command));
        }
    }
}
