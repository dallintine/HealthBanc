using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Domain.Models;
using HealthBanc.DTO.AuthenticationDTOs;
using HealthBanc.Helpers.Jwt_Authorization;
using HealthBanc.Infrastructure.Mail;
using HealthBanc.Response;
using HealthBanc.Services.EncryptionService;
using HealthBanc.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace HealthBanc.Services.Identity
{
    public class IdentityService
    {
        private readonly ILogger<IdentityService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEncryptAndDecrypt _encryptAndDecrypt;
        private readonly IEmailSender _emailSender;
        private readonly IApplicationUserRepository _userRepository;
        private readonly IClassOrRoleRepository _classOrRole;
        private readonly JwtSettings _jwtsettings;

        public IdentityService(ILogger<IdentityService> logger, UserManager<ApplicationUser> userManager,IEncryptAndDecrypt encryptAndDecrypt,IEmailSender emailSender,
             IOptions<JwtSettings> jwtsettings,IApplicationUserRepository userRepository,IClassOrRoleRepository classOrRole)
        {
            _logger = logger;
            _userManager = userManager;
            _encryptAndDecrypt = encryptAndDecrypt;
            _emailSender = emailSender;
            _userRepository = userRepository;
            _classOrRole = classOrRole;
            _jwtsettings = jwtsettings.Value;
        }

        public async Task<ResponseMessage> RegisterSuperAdmin(RegistrationViewModel registrationViewModel)
        {
            var checkUserEmail = await _userManager.FindByEmailAsync(registrationViewModel.EmailAddress);
            if (checkUserEmail == null)
            {
                var user = new ApplicationUser
                {
                    UserName = registrationViewModel.EmailAddress,
                    Email = registrationViewModel.EmailAddress,
                    FirstName = registrationViewModel.FirstName,
                    LastName = registrationViewModel.LastName,
                    PhoneNumber = registrationViewModel.PhoneNumber,
                    DateOfRegistration = DateTime.Now
                };


                var result = await _userManager.CreateAsync(user, registrationViewModel.Password);
                if (result.Succeeded)
                {
                    try
                    {
                        user.SuperAdminId = user.Id;
                        await _userManager.UpdateAsync(user);
                        await _userManager.AddToRoleAsync(user, "SuperAdmin");
                        await SendUserEmailVerificationAsync(user);
                        return new ResponseMessage
                        {
                            Message = "User Created Successfully,Please Check Email To Confirm Your Email Address And Login",
                            Status = true
                        };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "System Failed to Save SuperAdmin Security Question with UserId ${0}", user.Id);
                        return new ResponseMessage { Message = " Error occurred when trying to register user please try again latter or contact support",Status=false };
                    }
                }
                else
                {
                    return new ResponseMessage { Message = "Error occurred when trying to register user please try again latter or contact support" ,Status=false};
                }
            }
            return new ResponseMessage { Message = "Email Already Exist",Status = false };
        }

        public async Task<ResponseMessage> ConfirmEmail(string userId, string emailToken)
        {
            try
            {
                var decryptedUserId = _encryptAndDecrypt.DecryptString(HttpUtility.UrlDecode(userId), "hfahkbak78r32rg87griva..");
                var user = await _userRepository.FindByIdAsync(int.Parse(decryptedUserId));

                var decryptedEmailToken = _encryptAndDecrypt.DecryptString(HttpUtility.UrlDecode(emailToken), "hfahkbak78r32rg87griva..");
                var result = await _userManager.ConfirmEmailAsync(user, decryptedEmailToken);
                if (result.Succeeded)
                {
                    await Welcome(user);
                    return new ResponseMessage { Status = true };
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical("Error occurred when trying to confirm email please try again later : " + ex);
            }
            return new ResponseMessage { Message = "Error occurred when trying to confirm email please try again later" };
        }

        public async Task<ResponseMessage> Login(ApplicationUser user, LoginViewModel loginModel)
        {
            await _userManager.ResetAccessFailedCountAsync(user);

            try
            {
                //Generate Token
                var expirationTime = Convert.ToDouble(_jwtsettings.ExpirationTime);
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtsettings.Secret));
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(JwtRegisteredClaimNames.Sub, loginModel.EmailAddress),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                        new Claim("SuperAdminId", user.SuperAdminId.ToString()),
                        new Claim("AdminId", user.AdminId == null ? user.SuperAdminId.ToString() : user.AdminId.ToString()),
                        new Claim(ClaimTypes.Name, user.Id.ToString()),
                        new Claim("FirstName",user.FirstName),
                        new Claim("LastName",user.LastName),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim("LoggedOn", DateTime.Now.ToString()),
                    }),
                    SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature),
                    Issuer = _jwtsettings.Site,
                    Audience = _jwtsettings.Audience,
                    Expires = DateTime.UtcNow.AddMinutes(expirationTime)
                };
                //create the token 
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var loogedInResponse = new LoggedInResponseDTO
                {
                    Token = tokenHandler.WriteToken(token),
                    Username = user.Email,
                    Name = $"{user.FirstName} {user.LastName}",
                    ExpiryTime = DateTime.Now.AddMinutes(expirationTime),
                };
                return new ResponseMessage { Data = loogedInResponse, Status = true, Message = "User was logged in successfully" };
            }
            catch (Exception ex)
            {
                _logger.LogCritical("An error Occurred when " + user.Email + " tried to Login : " + ex);
            }
            return new ResponseMessage { Message = "Error occured please try again later" };
        }

        public async Task<ResponseMessage> ForgotPassword(ForgotPasswordViewModel forgotPassword)
        {
            var user = await _userManager.FindByNameAsync(forgotPassword.Username);
            if (user != null && await _userManager.IsEmailConfirmedAsync(user))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var encryptedToken = _encryptAndDecrypt.EncryptString(token, "hfahkbak78r32rg87griva..");

                var encryptedEmail = _encryptAndDecrypt.EncryptString(user.UserName, "hfahkbak78r32rg87griva..");

                var passwordResetLink = $"https://pharmmall.azurewebsites.net/v1/api/Identity/Reset_Password/?email={HttpUtility.UrlEncode(encryptedEmail)}&emailToken={HttpUtility.UrlEncode(encryptedToken)}";


                // Email the user the verification code
                try
                {
                    _emailSender.SendEmail(forgotPassword.Username,"d-f9c4860583d340f1bf25a355dc64caf2", passwordResetLink);
                    return new ResponseMessage { Message = "Please Check Your Mail For Further Instructions", Status = true };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falied to send Email Reset Mail to user ");
                }
            }
            return new ResponseMessage { Message = "Username Does Not Exist", Status = false };
        }

        public async Task<ResponseMessage> ResetPassword(string decryptedEmail, string decryptedEmailToken, ResetPasswordViewModel viewModel)
        {
            var user = await _userManager.FindByEmailAsync(decryptedEmail);
            try
            {
                PasswordVerificationResult passResult = _userManager.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash, viewModel.ConfirmPassword);
                if (passResult.Equals(PasswordVerificationResult.Failed))
                {
                    var userPassword = await _userManager.ResetPasswordAsync(user, decryptedEmailToken, viewModel.Password);
                    if (userPassword.Succeeded)
                    {
                        return new ResponseMessage { Message = "Password Changed Succefully", Status = true };
                    }
                    return new ResponseMessage { Message = "An error Occurred while trying to reset password", Data = userPassword.Errors };
                }
                else
                {
                    return new ResponseMessage { Message = "New Password Cant Be similar with Old Password" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical("Errror occured When Trying to reset " + user.UserName + " password : " + ex);
            }
            return new ResponseMessage { Message = "Error occured When Trying to reset user password" };
        }

        public async Task<ResponseMessage> CreateAdmin(CreateAdminRegViewModel regViewModel, string superAdminEmail, int superAdminId)
        {
            var superAdmin = await _userManager.FindByEmailAsync(superAdminEmail);

            var checkIfAdminExist = await _userManager.FindByNameAsync(regViewModel.Email);
            if (checkIfAdminExist == null)
            {
                var user = new ApplicationUser
                {
                    UserName = regViewModel.Email,
                    Email = regViewModel.Email,
                    FirstName = regViewModel.FirstName,
                    LastName = regViewModel.LastName,
                    DateOfRegistration = DateTime.Now,
                    SuperAdminId = superAdminId
                };                

                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    try
                    {
                        user.AdminId = user.Id;
                        await _userManager.UpdateAsync(user);
                        var role = await _classOrRole.GetRole(regViewModel.ClassOrRoleId);
                        await _userManager.AddToRoleAsync(user, role.Name);

                        // Generate an email verification code
                        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                        var encryptedEmail = _encryptAndDecrypt.EncryptString(user.UserName, "hfahkbak78r32rg87griva..");
                        var encryptedToken = _encryptAndDecrypt.EncryptString(token, "hfahkbak78r32rg87griva..");

                        // TODO: Replace with APIRoutes that will contain the static routes to use

                        var confirmationUrl = $"https://pharmmall.azurewebsites.net/v1/api/Identity/AdminReg/?email={HttpUtility.UrlEncode(encryptedEmail)}&emailToken={HttpUtility.UrlEncode(encryptedToken)}";
                        _emailSender.SendEmail(user.UserName, "d-d817b3791475490382e72d71567df4b2", confirmationUrl);


                        return new ResponseMessage
                        {
                            Message = "Admin Was Created Successfully,Please Check Email For Further Instruction",
                            Status = true
                        };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error Occurred While Trying to Create the Admin");
                    }
                }
                return new ResponseMessage { Message = "Error Occurred While Trying to Create the Admin" };
            }
            return new ResponseMessage { Message = "User Already Exist" };
        }

        public async Task<ResponseMessage> AdminReg(string email, string emailToken, AdminRegViewModel regViewModel)
        {
            var decryptedEmail = _encryptAndDecrypt.DecryptString(email, "hfahkbak78r32rg87griva..");
            var decryptedToken = _encryptAndDecrypt.DecryptString(emailToken, "hfahkbak78r32rg87griva..");

            var user = await _userManager.FindByEmailAsync(decryptedEmail);

            if (user == null)
            {
                return new ResponseMessage { Message = "User does not exist",ResponseCode = 2 };
            }
            var result = await _userManager.ConfirmEmailAsync(user, decryptedToken);

            if (result.Succeeded)
            {
                try
                {
                    var token = _userManager.GeneratePasswordResetTokenAsync(user);
                    var setPassword = await _userManager.ResetPasswordAsync(user, token.Result, regViewModel.NewPassword);
                    if (setPassword.Succeeded)
                    {
                        await Welcome(user);
                        return new ResponseMessage { Message = "Email Confirmed Successfully,Please Log In", Status = true };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occured While Trying to Create Admin Security Questions");
                }
                return new ResponseMessage { Message = "Error occured While Trying to Create Admin Security Questions" };
            }
            return new ResponseMessage { Message = "Error occured While Trying to Confirm User" };
        }

        private async Task SendUserEmailVerificationAsync(ApplicationUser user)
        {
            // Get the user details
            var userIdentity = await _userManager.FindByNameAsync(user.UserName);
            var encryptedUserIdentity = _encryptAndDecrypt.EncryptString(userIdentity.Id.ToString(), "hfahkbak78r32rg87griva..");

            if (userIdentity != null)
            {
                // Generate an email verification code
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var encryptedToken = _encryptAndDecrypt.EncryptString(token, "hfahkbak78r32rg87griva..");

                var confirmationUrl = $"https://healthbanc.sterlingapps.p.azurewebsites.net/v1/api/Identity/ConfirmEmail/?userId={HttpUtility.UrlEncode(encryptedUserIdentity)}&emailToken={HttpUtility.UrlEncode(encryptedToken)}";


                // Email the user the verification code
                try
                {
                    _emailSender.SendEmail(user.UserName, "d-d817b3791475490382e72d71567df4b2", confirmationUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falied to send Email Verification Mail to user From Method SendUserEmailVerificationAsync()");
                }
            }
        }
        private async Task Welcome(ApplicationUser user)
        {
            // Get the user details
            var userIdentity = await _userManager.FindByNameAsync(user.UserName);
            if (userIdentity != null)
            {

                // TODO: Replace with APIRoutes that will contain the static routes to use

                var confirmationUrl = $"https://pharmmall.azurewebsites.net/signin";

                // Email the user the verification code
                try
                {
                    _emailSender.SendEmail(user.UserName, "d-d817b3791475490382e72d71567df4b2", "confirmationUrl");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falied to send Email Verification Mail to Admin From Method VerifyAdmin()");
                }
            }
        }
    }
}
