using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Application.DTO;
using Application.Helpers.ThirdPartyAPI;
using Application.Interfaces;
using Application.Services.Identity;
using Application.ViewModels.UserReg_Login;
using DataAccess;
using DataAccess.General.Interfaces;
using DataAccess.HealthInsured.Interfaces;
using DataAccess.Logs.Interfaces;
using Domain.Enums;
using Domain.Models;
using Domain.Models.ReportAndLogs;
using HealthBanc.DTO.AuthenticationDTOs;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using UAParser;

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
        private readonly IPasswordHasher _passwordHasher;
        private readonly IRepositoryWrapper _repoWrapper;

        private AppEndpoint Options { get; }
        public StringValues agent;
        public string IpAddress;

        public IdentityController(ILogger<IdentityController> logger, IdentityService identityService, UserManager<ApplicationUser> userManager, IEncryptAndDecrypt encryptAndDecrypt
            ,IPasswordHasher passwordHasher, IRepositoryWrapper repoWrapper,IOptions<AppEndpoint> optionAccessor, IHttpContextAccessor accessor)
        {
            Options = optionAccessor.Value;
            _logger = logger;
            _identityService = identityService;
            _userManager = userManager;
            _encryptAndDecrypt = encryptAndDecrypt;
            _passwordHasher = passwordHasher;
            _repoWrapper = repoWrapper;
            agent = accessor.HttpContext.Request.Headers["User-Agent"];
            IpAddress = accessor.HttpContext.Connection.RemoteIpAddress.ToString();
        }

        /// <summary>
        /// Log user out
        /// </summary>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        public async Task<IActionResult> LogOut()
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                int id = int.Parse(userId);
                var session = await _repoWrapper.UserSession.GetByUserId_Device(id, IpAddress);
                if (session != null)
                {
                    _repoWrapper.UserSession.Delete(session);
                    await _repoWrapper.Save();
                }
            }           
            return Ok(new ResponseMessage {Status=true, Message= "Log out successful" });
        }

        ///<summary>
        ///This Creates The User
        ///</summary>        
        ///<param name = "registrationViewModel" ></param >
        ///<param name="app"></param>
        ///<response code="200">Success : User Created Successfully,Please Check Email To Confirm Your Email Address And Login</response>
        ///<reponse code = "400" > Error : List of Input Validation Errors</reponse>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public async Task<IActionResult> RegisterUser([FromBody] RegistrationViewModel registrationViewModel , string app)
        {
            if (ModelState.IsValid)
            {
                var response = await _identityService.RegisterUser(registrationViewModel,app);
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
                errors.Add(new ResponseMessage() { Message = error, Status = false });
                _logger.LogInformation(error);
            }
            return BadRequest(errors);
        }

        /// <summary>
        /// Social Media registration Link
        /// </summary>
        /// <param name="registrationViewModel"></param>
        /// <param name="app"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public async Task<IActionResult> SocialMediaRegistrationLink([FromBody] RegistrationViewModel registrationViewModel, string app)
        {
            if (ModelState.IsValid)
            {
                var userAgent = agent;
                string uaString = Convert.ToString(userAgent[0]);
                var uaParser = Parser.GetDefault();
                ClientInfo c = uaParser.Parse(uaString);
                var browser = c.UA.ToString();
                var deviceIp = IpAddress;
                var response = await _identityService.SocialMediaRegistrationLink(registrationViewModel,app,browser, deviceIp);
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
                errors.Add(new ResponseMessage() { Message = error, Status = false });
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
        /// <param name="app"></param>
        /// <response code="200">Success : Redirect to Login</response>
        ///<reponse code="400">Error : List of Input Validation Errors</reponse>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        public async Task<IActionResult> ConfirmEmail(string userId, string emailToken,string app)
        {
            if (ModelState.IsValid)
            {
                if (userId is null || emailToken is null)
                {
                    return BadRequest(new ResponseMessage { Message = "email or email token can not be null" });
                }
                var response = await _identityService.ConfirmEmail(userId, emailToken);
                if (response.Status == true)
                {
                    if(app == HealthbancApps.HealthInsured.ToString())
                    {
                        return Redirect(Options.APIUri.HealthInsuredSignin);
                    }
                    return Redirect(Options.APIUri.HealthBancSignIn);
                }
                if(response.ResponseCode  == 23)
                {
                    return Redirect(Options.APIUri.HealthBancResendEmail);
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
                errors.Add(new ResponseMessage() { Message = error, Status = false });
            }
            return BadRequest(errors);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> ResendConfirmationLink(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || user.UniqueUsername != null)
            {
                return BadRequest(new ResponseMessage {Message = "User does not exist" , Status=false });
            }
            if (user.EmailConfirmed == true)
            {
                return BadRequest(new ResponseMessage { Message = "Your email address has previously been confirmed, kindly proceed to login", Status = false });
            }
            var confirmResult = await _identityService.SendUserEmailVerificationAsync(user,null);
            if (confirmResult.Status == true)
            {
                return Ok(new ResponseMessage { Message = "Link was sent successfully, kindly check your email", Status = true });
            }
            return BadRequest(new ResponseMessage { Message = confirmResult.Message, Status = false });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="loginViewModel"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        public async Task<ActionResult> Login([FromBody] LoginViewModel loginViewModel)
        {
            if (ModelState.IsValid)
            {
                //get the user
                var userAgent = agent;
                string uaString = Convert.ToString(userAgent[0]);
                var uaParser = Parser.GetDefault();
                ClientInfo c = uaParser.Parse(uaString);
                var browser = c.UA.ToString();
                var deviceIp = IpAddress;

                var user = await _userManager.FindByEmailAsync(loginViewModel.EmailAddress);

                if (user == null || user.IsDeleted == true) return NotFound(new ResponseMessage { Message = "User detail is invalid, please try again with correct details" +
                    "", Status = false });


                if (user.EmailConfirmed == false) return Unauthorized(new ResponseMessage { Message = "Please confirm your email address", Status = false });

                if (user.LockoutEnd != null)
                {
                    return Unauthorized(new ResponseMessage { Message = "Your account has been locked, you exceeded the maximum failed password attempt. Kindly unlock your account by resetting your password", Status = false });
                }

                //check that the user password is correct
                if (await _userManager.CheckPasswordAsync(user, loginViewModel.Password))
                {
                    var response = await _identityService.Login2(user,browser, deviceIp);
                    if (response.Status != true)
                    {
                        return BadRequest(response);
                    }
                    return Ok(response);
                }
                //increase access failed count
                await _userManager.AccessFailedAsync(user);
                return Unauthorized(new ResponseMessage { Message = "User detail is invalid, please try again with correct details.", Status = false });
            }
            //return validation errors
            var errors = new List<ResponseMessage>();
            var errorList = ModelState.Values.SelectMany(m => m.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            foreach (var error in errorList)
            {
                errors.Add(new ResponseMessage() { Message = error, Status = false });
            }
            return BadRequest(errors);
        }

        [ProducesResponseType(200, Type = typeof(ResponseMessage<LoggedInResponseDTO>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage<LoggedInResponseDTO>))]
        [HttpPost("[action]")]
        public async Task<IActionResult> RefreshToken(RefreshTokenViewModel refreshModel)
        {
            var userAgent = agent;
            string uaString = Convert.ToString(userAgent[0]);
            var uaParser = Parser.GetDefault();
            ClientInfo c = uaParser.Parse(uaString);
            var browser = c.UA.ToString();
            var deviceIp = IpAddress;
            var authResponse = await _identityService.Refresh2(refreshModel,browser,deviceIp);
            if (!authResponse.Status)
            {
                return BadRequest(authResponse);
            }
            return Ok( authResponse);
        }

        /// <summary>
        /// Request user email to reset his password
        /// </summary>
        /// <param name="forgotPassword"></param>
        /// <param name="app"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [ProducesResponseType(404, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordViewModel forgotPassword, string app)
        {
            if (ModelState.IsValid)
            {
                var response = await _identityService.ForgotPassword(forgotPassword,app);
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
                errors.Add(new ResponseMessage() { Message = error, Status = false });
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
                if (email is null || emailToken is null)
                {
                    return BadRequest(new ResponseMessage { Message = "email or email token can not be null" });
                }

                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return NotFound(new ResponseMessage { Message = "User with the email could not be found" });
                }

                var response = await _identityService.ResetPassword(email, emailToken, viewModel);

                if (response.Status == true)
                {
                    return Ok(response);
                }
                if(response.ResponseCode == 23)
                {
                    return BadRequest(response);
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
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordViewModel changePassword)
        {
            if (ModelState.IsValid)
            {
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                var user = await _userManager.FindByIdAsync(userId);

                if (user != null)
                {
                    PasswordVerificationResult passResult = _userManager.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash, changePassword.ConfirmPassword);
                    if (passResult.Equals(PasswordVerificationResult.Failed))
                    {
                        if(user.HashedPasswordHistory != null)
                        {
                            var hashedPassword = user.HashedPasswordHistory.Split(",").ToList();
                            if (hashedPassword.LastOrDefault() == "")
                            {
                                hashedPassword.RemoveAt(hashedPassword.Count - 1);
                            }
                            foreach (var item in hashedPassword)
                            {
                                var (Verified, NeedsUpgrade) = _passwordHasher.Check(item, changePassword.ConfirmPassword);
                                if (Verified == true)
                                {
                                    return BadRequest(new ResponseMessage { Message = "The password you entered has been used before,please try another" });
                                }
                                var checkForValidPassword = _passwordHasher.Check(item, changePassword.ConfirmPassword);
                            }
                        }                      
                        
                        var userPassword = await _userManager.ChangePasswordAsync(user, changePassword.Password, changePassword.NewPassword);
                        if (userPassword.Succeeded)
                        {
                            var passwordHashed = _passwordHasher.Hash(changePassword.NewPassword);
                            if (user.HashedPasswordHistory != null)
                            {
                                var hashedPassword = user.HashedPasswordHistory.Split(",").ToList();                                
                                if (hashedPassword.LastOrDefault() == "")
                                {
                                    hashedPassword.RemoveAt(hashedPassword.Count - 1);
                                    if(hashedPassword.Count >3)
                                    {
                                        hashedPassword.RemoveAt(0);                                       
                                        var newPaswordHash = string.Join(",", hashedPassword);
                                        user.HashedPasswordHistory =  $"{newPaswordHash},{passwordHashed},";
                                        await _userManager.UpdateAsync(user);
                                        var passwordChangehistory2 = new PasswordChangeHistory(user.Id, user.Email, true, false);
                                        _repoWrapper.PasswordChange.Create(passwordChangehistory2);
                                        await _repoWrapper.Save();
                                        return Ok(new ResponseMessage { Message = "Password changed successfully", Status = true });
                                    }
                                }                                
                            }
                            user.HashedPasswordHistory = user.HashedPasswordHistory += passwordHashed + ",";
                            await _userManager.UpdateAsync(user);
                            var passwordChangehistory = new PasswordChangeHistory(user.Id, user.Email, true, false);
                            _repoWrapper.PasswordChange.Create(passwordChangehistory);
                            await _repoWrapper.Save();
                            return Ok(new ResponseMessage { Message = "Password changed succesfully", Status = true });
                        }
                        return BadRequest(new ResponseMessage { Message = "Current password is wrong,please input correct one or reset password" });
                    }
                    else
                    {
                        return BadRequest(new ResponseMessage { Message = "New password can not be similar with old password" });
                    }
                };
                return BadRequest(new ResponseMessage { Message = "User does not exist" });
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

        [HttpGet("[action]")]
        [Authorize]
        public async Task<IActionResult> GetUserServices()
        {
            try
            {
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int Id = int.Parse(userId);
                var user = await _repoWrapper.ApplicationUser.FindByIdAsync(Id);
                if (user != null)
                {
                    return Ok(new ResponseMessage { Data = user.ServiceUsed, Status = true, Message = "Service used was fetched successfully" });
                }
                return NotFound(new ResponseMessage { Message = "User was not found" });
            }
            catch(Exception ex)
            {
                return BadRequest(new ResponseMessage { Message = "An error occurred while trying to get service used by user : " +ex.Message.ToString() });
            }           
        }

    }
}

