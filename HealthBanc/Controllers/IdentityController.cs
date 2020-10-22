using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Domain.Models;
using HealthBanc.Domain.Models.ReportAndLogs;
using HealthBanc.DTO.AuthenticationDTOs;
using HealthBanc.Helpers.ThirdPartyAPI;
using HealthBanc.Infrastructure.Mail;
using HealthBanc.Response;
using HealthBanc.Services.EncryptionService;
using HealthBanc.Services.Identity;
using HealthBanc.Services.PasswordManager;
using HealthBanc.ViewModels;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
        private readonly IApplicationUserRepository _userRepository;
        private readonly IEmailSender _emailSender;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IPasswordChangeRepository _passwordChangeRepository;
        private readonly IUserLogin_LogoutLogRepository _logoutLogRepository;
        private AppEndpoint Options { get; }

        public IdentityController(ILogger<IdentityController> logger, IdentityService identityService, UserManager<ApplicationUser> userManager, IEncryptAndDecrypt encryptAndDecrypt,
            IApplicationUserRepository userRepository, IEmailSender emailSender,IPasswordHasher passwordHasher, IPasswordChangeRepository passwordChangeRepository,
            IUserLogin_LogoutLogRepository logoutLogRepository, IOptions<AppEndpoint> optionAccessor)
        {
            Options = optionAccessor.Value;
            _logger = logger;
            _identityService = identityService;
            _userManager = userManager;
            _encryptAndDecrypt = encryptAndDecrypt;
            _userRepository = userRepository;
            _emailSender = emailSender;
            _passwordHasher = passwordHasher;
            _passwordChangeRepository = passwordChangeRepository;
            _logoutLogRepository = logoutLogRepository;
        }

        ///<summary>
        ///This Creates The User
        ///</summary>        
        ///<param name = "registrationViewModel" ></param >
        ///<response code="200">Success : User Created Successfully,Please Check Email To Confirm Your Email Address And Login</response>
        ///<reponse code = "400" > Error : List of Input Validation Errors</reponse>
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
        /// <response code="200">Success : Redirect to Login</response>
        ///<reponse code="400">Error : List of Input Validation Errors</reponse>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpGet("[action]")]
        public async Task<IActionResult> ConfirmEmail(string userId, string emailToken)
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
            var confirmResult = await _identityService.SendUserEmailVerificationAsync(user);
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
                var user = await _userManager.FindByEmailAsync(loginViewModel.EmailAddress);

                if (user == null || user.IsDeleted == true) return NotFound(new ResponseMessage { Message = "Account with this email does not exist", Status = false });


                if (user.EmailConfirmed == false) return Unauthorized(new ResponseMessage { Message = "Please Confirm Your Email Address", Status = false });

                if (user.LockoutEnd != null)
                {
                    return Unauthorized(new ResponseMessage { Message = "Your account has been locked, you exceeded the maximum failed password attempt. Kindly unlock your account by clicking on the forgot password link", Status = false });
                }

                //check that the user is not null and that his password is correct
                if (await _userManager.CheckPasswordAsync(user, loginViewModel.Password))
                {
                    var response = await _identityService.Login2(user, loginViewModel);
                    if (response.Status != true)
                    {
                        return BadRequest(response);
                    }
                    return Ok(response);
                }
                await _userManager.AccessFailedAsync(user);
                var loginLog = new UserLogin_LogoutLog(user.Id, user.Email, true, false, true);
                _logoutLogRepository.Create(loginLog);
                await _logoutLogRepository.Save();
                return Unauthorized(new ResponseMessage { Message = "Password is invalid, please try again with correct details.", Status = false });
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
            var authResponse = await _identityService.Refresh2(refreshModel);
            if (!authResponse.Status)
            {
                return BadRequest(authResponse);
            }
            return Ok( authResponse);
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
                var decryptedEmail = _encryptAndDecrypt.DecryptString(email, "hfahkbak78r32rg87griva..");
                var decryptedEmailToken = _encryptAndDecrypt.DecryptString(emailToken, "hfahkbak78r32rg87griva..");

                var user = await _userManager.FindByEmailAsync(decryptedEmail);
                if (user == null)
                {
                    return NotFound(new ResponseMessage { Message = "User with the email could not be found" });
                }

                var response = await _identityService.ResetPassword(decryptedEmail, decryptedEmailToken, viewModel);

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
                                var checkForValidPassword = _passwordHasher.Check(item, changePassword.ConfirmPassword);
                                if (checkForValidPassword.Verified == true)
                                {
                                    return BadRequest(new ResponseMessage { Message = "The password you entered has been used before,please try another" });
                                }
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
                                        _passwordChangeRepository.Create(passwordChangehistory2);
                                        await _passwordChangeRepository.Save();
                                        return Ok(new ResponseMessage { Message = "Password Changed Succefully", Status = true });
                                    }
                                }                                
                            }
                            user.HashedPasswordHistory = user.HashedPasswordHistory += passwordHashed + ",";
                            await _userManager.UpdateAsync(user);
                            var passwordChangehistory = new PasswordChangeHistory(user.Id, user.Email, true, false);
                            _passwordChangeRepository.Create(passwordChangehistory);
                            await _passwordChangeRepository.Save();
                            return Ok(new ResponseMessage { Message = "Password Changed Succefully", Status = true });
                        }
                        return BadRequest(new ResponseMessage { Message = "Current Password is Wrong,Please Input Corrrect One,Or Reset Password" });
                    }
                    else
                    {
                        return BadRequest(new ResponseMessage { Message = "New Password Cant Be similar with Old Password" });
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
                var user = await _userRepository.FindByIdAsync(Id);
                if (user != null)
                {
                    return Ok(new ResponseMessage { Data = user.ServiceUsed, Status = true, Message = "Service used was fetched successfully" });
                }
                return NotFound(new ResponseMessage { Message = "User was not found" });
            }
            catch(Exception ex)
            {
                return BadRequest(new ResponseMessage { Message = "An error occurred while trying to get service used by user" });
            }           
        }

    }
}



///// <summary>
///// This Creates Admin Requires SuperAdmin Rights
///// </summary>
/////<response code="200">Success : Admin Was Created Successfully,User Should Check Email For Further Instruction</response>
/////<reponse code="400">Error : List of Input Validation Errors</reponse>
//[ProducesResponseType(200, Type = typeof(ResponseMessage))]
//[ProducesResponseType(400, Type = typeof(ResponseMessage))]
//[HttpPost("[action]")]
//[Authorize(Roles = "SuperAdmin")]
//public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRegViewModel regViewModel)
//{
//    if (ModelState.IsValid)
//    {
//        string superAdminEmail = User.FindFirst(ClaimTypes.Email)?.Value;
//        string getSuperAdminId = User.FindFirst("SuperAdminId")?.Value;
//        int superAdminId = int.Parse(getSuperAdminId);
//        var response = await _identityService.CreateAdmin(regViewModel, superAdminEmail, superAdminId);

//        if (response.Status == true)
//        {
//            return Ok(response);
//        }
//        return BadRequest(response);
//    }
//    //return validation errors
//    var errors = new List<ResponseMessage>();
//    var errorList = ModelState.Values.SelectMany(m => m.Errors)
//        .Select(e => e.ErrorMessage)
//        .ToList();
//    foreach (var error in errorList)
//    {
//        errors.Add(new ResponseMessage() { Message = error });
//    }
//    return BadRequest(errors);
//}

///// <summary>
///// This Confirms the Created AdminEmail
///// </summary>
///// <param name="email">Encoded String:Takes the Useremail as query Parameter</param>
///// <param name="emailToken">Encoded String:Takes the EmailToken also as query Parameter</param>
///// <param name="regViewModel"></param>
///// <response code="200">Success : Email Confirmed Successfully Please Login</response>
/////<reponse code="400">Error : List of Input Validation Errors</reponse>
//[ProducesResponseType(200, Type = typeof(ResponseMessage))]
//[ProducesResponseType(400, Type = typeof(ResponseMessage))]
//[HttpPost("[action]")]
//public async Task<IActionResult> AdminReg(string email, string emailToken, [FromBody] AdminRegViewModel regViewModel)
//{
//    if (ModelState.IsValid)
//    {
//        if (email is null || emailToken is null)
//        {
//            return BadRequest(new ResponseMessage { Message = "email or email token can not be null" });
//        }
//        var response = await _identityService.AdminReg(email, emailToken, regViewModel);
//        if (response.Status == true)
//        {
//            return Ok(response);
//        }
//        if (response.ResponseCode == 2)
//        {
//            return BadRequest(response);
//        }
//        if (response.ResponseCode == 23)
//        {
//            return BadRequest(response);
//        }
//        return BadRequest(response);
//    }
//    //return validation errors
//    var errors = new List<ResponseMessage>();
//    var errorList = ModelState.Values.SelectMany(m => m.Errors)
//        .Select(e => e.ErrorMessage)
//        .ToList();
//    foreach (var error in errorList)
//    {
//        errors.Add(new ResponseMessage() { Message = error });
//    }
//    return Unauthorized(errors);
//}