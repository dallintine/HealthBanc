using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using HealthBanc.Domain.Models;
using HealthBanc.Response;
using HealthBanc.Services.EncryptionService;
using HealthBanc.Services.Identity;
using HealthBanc.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HealthBanc.Controllers
{
    [Route("v1/api/[controller]")]
    [ApiController]
    public class IdentityController : ControllerBase
    {
        private readonly ILogger<IdentityController> _logger;
        private readonly IdentityService _identityService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEncryptAndDecrypt _encryptAndDecrypt;

        public IdentityController(ILogger<IdentityController> logger, IdentityService identityService, UserManager<ApplicationUser> userManager, IEncryptAndDecrypt encryptAndDecrypt)
        {
            _logger = logger;
            _identityService = identityService;
            _userManager = userManager;
            _encryptAndDecrypt = encryptAndDecrypt;
        }


        /// <summary>
        /// This Creates The User
        /// </summary>        
        /// <param name="registrationViewModel"></param>
        ///<response code="200">Success : User Created Successfully,Please Check Email To Confirm Your Email Address And Login </response>
        ///<reponse code="400">Error : List of Input Validation Errors</reponse>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public async Task<IActionResult> RegisterUser([FromBody] RegistrationViewModel registrationViewModel)
        {
            if (ModelState.IsValid)
            {
                var response = await _identityService.RegisterSuperAdmin(registrationViewModel);
                if (response.Status == true)
                {
                    return Ok(response);
                }
                return BadRequest(response);
            }
            //return validation errors
            var errors = new List<ResponseMessage>();
            var errorList = ModelState.Values.SelectMany(m => m.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            foreach (var error in errorList)
            {
                errors.Add(new ResponseMessage() { Message = error,Status=false });
                _logger.LogInformation(error);
            }
            return BadRequest(errors);
        }

        //WORKING1
        /// <summary>
        /// This Confirms the UserEmail
        /// </summary>
        /// <param name="userId">Encoded String:Takes the UserID as query Parameter</param>
        /// <param name="emailToken">Encoded String:Takes the EmailToken also as query Parameter</param>
        /// <response code="200">Success : Redirect to Login</response>
        ///<reponse code="400">Error : List of Input Validation Errors</reponse>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        public async Task<IActionResult> ConfirmEmail(string userId, string emailToken)
        {
            if (ModelState.IsValid)
            {
                var response = await _identityService.ConfirmEmail(userId, emailToken);
                if (response.Status == true)
                {
                    return Redirect("https://pharmmall.azurewebsites.net/signin");
                }
                return BadRequest(response);
            }
            //return validation errors
            var errors = new List<ResponseMessage>();
            var errorList = ModelState.Values.SelectMany(m => m.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            foreach (var error in errorList)
            {
                errors.Add(new ResponseMessage() { Message = error,Status =false });
            }
            return BadRequest(errors);
        }


        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(401, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public async Task<ActionResult<ResponseMessage>> Login([FromBody] LoginViewModel loginViewModel)
        {
            if (ModelState.IsValid)
            {
                //get the user
                var user = await _userManager.FindByEmailAsync(loginViewModel.EmailAddress);

                if (user == null || user.IsDeleted == true) return NotFound(new ResponseMessage { Message = "Account with this email does not exist" , Status =false });


                if (user.EmailConfirmed == false) return Unauthorized(new ResponseMessage { Message = "Please Confirm Your Email Address" , Status = false });

                if (user.LockoutEnd != null)
                {
                    return Unauthorized(new ResponseMessage { Message = "Your Account Has Been Locked,Please Contact Support" , Status = false });
                }

                //check that the user is not null and that his password is correct
                if (await _userManager.CheckPasswordAsync(user, loginViewModel.Password))
                {
                    var response = await _identityService.Login(user, loginViewModel);
                    if (response.Status != true)
                    {
                        return BadRequest(response);
                    }
                    return Ok(response);
                }
                await _userManager.AccessFailedAsync(user);
                return Unauthorized(new ResponseMessage { Message = "Username or password invalid, please try again with correct details." , Status = false });
            }
            //return validation errors
            var errors = new List<ResponseMessage>();
            var errorList = ModelState.Values.SelectMany(m => m.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            foreach (var error in errorList)
            {
                errors.Add(new ResponseMessage() { Message = error , Status = false });
            }
            return BadRequest(errors);
        }

        //WORKING1
        /// <summary>
        /// Request user email to reset his password
        /// </summary>
        /// <response code = "200">Success : Please Check Your Mail For Further Instructions</response>
        /// <response code = "404">Error : Username Does Not Exist </response>
        /// <response code = "400">Error : List of Input Validation Errors</response>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordViewModel forgotPassword)
        {
            if (ModelState.IsValid)
            {
                var response = await _identityService.ForgotPassword(forgotPassword);
                if (response.Status == true)
                {
                    return Ok(response);
                }
                return BadRequest(response);
            }
            var errors = new List<ResponseMessage>();
            var errorList = ModelState.Values.SelectMany(m => m.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            foreach (var error in errorList)
            {
                errors.Add(new ResponseMessage() { Message = error , Status = false });
            }
            return BadRequest(errors);
        }

        //WORKING1
        /// <summary>
        /// Resets the user Password
        /// </summary>
        /// <param name ="email">Encoded String:Takes the Email as query Parameter</param>
        /// <param name ="emailToken">Encoded String:Takes the EmailToken as query Parameter</param>
        /// <param name="viewModel"></param>
        /// <response code ="200">Succcess : Password Changed Succefully</response>
        /// <response code ="404">Failed : SecurityAnswer Does Not Match Contact Our Support Team</response>
        /// <response code="400">Failed:List of Input Validation Errors</response>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public async Task<IActionResult> ResetPassword(string email, string emailToken, [FromBody] ResetPasswordViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var decryptedEmail = _encryptAndDecrypt.DecryptString(HttpUtility.UrlDecode(email), "hfahkbak78r32rg87griva..");
                var decryptedEmailToken = _encryptAndDecrypt.DecryptString(HttpUtility.UrlDecode(emailToken), "hfahkbak78r32rg87griva..");

                var user = await _userManager.FindByEmailAsync(decryptedEmail);
                if(user == null)
                {
                    return NotFound(new ResponseMessage { Message = "User with the email could not be found" });
                }

                var response = await _identityService.ResetPassword(decryptedEmail, decryptedEmailToken, viewModel);

                if (response.Status == true)
                {
                    return Ok(response);
                }
                return BadRequest(response);
            }
            //return validation errors
            var errors = new List<ResponseMessage>();
            var errorList = ModelState.Values.SelectMany(m => m.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            foreach (var error in errorList)
            {
                errors.Add(new ResponseMessage() { Message = error });
            }
            return BadRequest(errors);
        }

        //WORKING1
        /// <summary>
        /// Changes the user password
        /// </summary>
        /// <returns>returns LoggedInResponse Object</returns>
        /// <response code="200">Success :Password Changed Succefully</response>
        /// <response code="401">Error : Current Password is Wrong,Please Input Corrrect One,Or Reset Password</response>
        /// <response code="400">Error :List of Input Validation Errors </response>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordViewModel changePassword)
        {
            if (ModelState.IsValid)
            {
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                var user = await _userManager.FindByIdAsync(userId);

                if (user != null)
                {
                    var userPassword = await _userManager.ChangePasswordAsync(user, changePassword.Password, changePassword.NewPassword);
                    if (userPassword.Succeeded)
                    {
                        return Ok(new ResponseMessage { Message = "Password Changed Succefully",Status=true });
                    }
                    return Unauthorized(new ResponseMessage { Message = "Current Password is Wrong,Please Input Corrrect One,Or Reset Password"});
                };
                return BadRequest(new ResponseMessage { Message = "User does not exist"});
            }
            //return validation errors
            var errors = new List<ResponseMessage>();
            var errorList = ModelState.Values.SelectMany(m => m.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            foreach (var error in errorList)
            {
                errors.Add(new ResponseMessage() { Message = error });
            }
            return BadRequest(errors);
        }
    }
}