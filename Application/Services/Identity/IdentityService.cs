using Application.DTO;
using Application.Helpers;
using Application.Helpers.Jwt_Authorization;
using Application.Helpers.ThirdPartyAPI;
using Application.Interfaces;
using Application.ViewModels.UserReg_Login;
using AutoMapper;
using DataAccess;
using DataAccess.General.Interfaces;
using DataAccess.HealthInsured.Interfaces;
using DataAccess.Logs.Interfaces;
using Domain.Models;
using Domain.Models.ReportAndLogs;
using HealthBanc.DTO.AuthenticationDTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Persistence;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Application.Services.Identity
{
    public class IdentityService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEncryptAndDecrypt _encryptAndDecrypt;
        private readonly IEmailSender _emailSender;
        private readonly IRepositoryWrapper _repoWrapper;
        private readonly JwtSettings _jwtsettings;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly IPasswordHasher _passwordHasher;
        private AppEndpoint Options { get; }



        public IdentityService( UserManager<ApplicationUser> userManager, IEncryptAndDecrypt encryptAndDecrypt, IEmailSender emailSender,
             IOptions<JwtSettings> jwtsettings, IRepositoryWrapper repoWrapper,
              TokenValidationParameters tokenValidationParameters,IPasswordHasher passwordHasher, IOptions<AppEndpoint> optionAccessor)
        {
            Options = optionAccessor.Value;
            _userManager = userManager;
            _encryptAndDecrypt = encryptAndDecrypt;
            _emailSender = emailSender;
            _repoWrapper = repoWrapper;
            _jwtsettings = jwtsettings.Value;
            _tokenValidationParameters = tokenValidationParameters;
            _passwordHasher = passwordHasher;     
        }

        public async Task<ResponseMessage> RegisterUser(RegistrationViewModel registrationViewModel)
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
                    await _userManager.UpdateAsync(user);
                    await _userManager.AddToRoleAsync(user, "SuperAdmin");
                    await SendUserEmailVerificationAsync(user);
                    var password = _passwordHasher.Hash(registrationViewModel.Password);
                    user.HashedPasswordHistory = $"{password},";
                    await _userManager.UpdateAsync(user);
                    await ProcessInsuranceUserId(user, user.Email);
                    return new ResponseMessage
                    {
                        Message = "User Created Successfully,Please Check Email To Confirm Your Email Address And Login",
                        Status = true
                    };
                }
                else
                {
                    return new ResponseMessage { Message = result.Errors.FirstOrDefault().Description ,Status=false};
                }
            }
            return new ResponseMessage { Message = "Email Already Exist",Status = false };
        }

        public async Task<ResponseMessage> SocialMediaRegistrationLink(RegistrationViewModel registrationViewModel)
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
                    DateOfRegistration = DateTime.Now,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, registrationViewModel.Password);
                if (result.Succeeded)
                {
                    await _userManager.UpdateAsync(user);
                    await _userManager.AddToRoleAsync(user, "SuperAdmin");                    
                    await SendUserEmailVerificationAsync(user);
                    var password = _passwordHasher.Hash(registrationViewModel.Password);
                    user.HashedPasswordHistory = $"{password},";
                    await _userManager.UpdateAsync(user);
                    await ProcessInsuranceUserId(user, user.Email);
                    var authResponse = await GetAuthenticationResultForUserAsync(user);
                    if (authResponse.Success) return new ResponseMessage { Data = authResponse, Status = true, Message = "User was logged in successfully" };
                    return new ResponseMessage
                    {
                        Message = "User Created Successfully,Please Check Email To Confirm Your Email Address And Login",
                        Status = true
                    };
                }
                else
                {
                    return new ResponseMessage { Message = result.Errors.FirstOrDefault().Description, Status = false };
                }
            }
            return new ResponseMessage { Message = "Email Already Exist", Status = false };
        }

        public async Task<ResponseMessage<ApplicationUser>> RegisterUserWithoutPassword(ApplicationUser appUser)
        {
            var result = await _userManager.CreateAsync(appUser);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(appUser, "SuperAdmin");
                await _userManager.UpdateAsync(appUser);
                return new ResponseMessage<ApplicationUser>
                {
                    Data = appUser,
                    Message = "User Created Successfully,Please Check Email To Confirm Your Email Address And Login",
                    Status = true
                };
            }
            return new ResponseMessage<ApplicationUser> { Message = result.Errors.FirstOrDefault().Description, Status = false };

        }

        public async Task<ResponseMessage> ConfirmEmail(string userId, string emailToken)
        {
            var user = await _repoWrapper.ApplicationUser.FindByIdAsync(int.Parse(userId));
            var result = await _userManager.ConfirmEmailAsync(user, emailToken);
            if (result.Succeeded)
            {
                return new ResponseMessage { Status = true };
            }
            else if (!result.Succeeded && result.Errors.Any(x => x.Code == "InvalidToken"))
            {
                return new ResponseMessage { Message = "Invalid Token", ResponseCode = 23 };
            }
            return new ResponseMessage { Message = result.Errors.FirstOrDefault().Description, Data = result.Errors };
        }

        public async Task<ResponseMessage> Login2(ApplicationUser user)
        {
            await _userManager.ResetAccessFailedCountAsync(user);

            var authResponse = await GetAuthenticationResultForUserAsync(user);
            if (authResponse.Success) return new ResponseMessage { Data = authResponse, Status = true, Message = "User was logged in successfully" };

            return new ResponseMessage { Data = authResponse, Message = "Error occured please try again later"};
        }

        public async Task<ResponseMessage<LoggedInResponseDTO>> Refresh2(RefreshTokenViewModel refreshToken)
        {
            var principal = GetPrincipalFromExpiredToken(refreshToken.Token);
            var username = principal.Identity.Name; //this is mapped to the Name claim by default
            var user = await _repoWrapper.ApplicationUser.FindByIdAsync(int.Parse(username));
            if (user == null)
            {
                return new ResponseMessage<LoggedInResponseDTO> { Message = "User could not be fetched" };
            }
            if(user.RefreshToken != refreshToken.RefreshToken)
            {
                return new ResponseMessage<LoggedInResponseDTO> { Message = "Invalid refresh token" };
            }
            if(user.RefreshTokenExpiryTime <= DateTime.Now)
            {
                return new ResponseMessage<LoggedInResponseDTO> { Message = "This refresh token has expired" };
            }
            var newRefreshToken = GenerateRefreshToken();
            user.RefreshToken = newRefreshToken;
            _repoWrapper.ApplicationUser.Update(user);
            await _repoWrapper.Save();

            var authResponse = await GetAuthenticationResultForUserAsync(user);
            if (authResponse.Success) return new ResponseMessage<LoggedInResponseDTO> { Data = authResponse, Status = true, Message = "User was logged in successfully" };

            return new ResponseMessage<LoggedInResponseDTO> { Data = authResponse, Message = "Error occured, please try again later" };
        }

        private async Task<LoggedInResponseDTO> GetAuthenticationResultForUserAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            //Generate Token
            var expirationTime = Convert.ToDouble(_jwtsettings.ExpirationTime);
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtsettings.Secret));
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Name, user.Id.ToString()),
                    new Claim("FirstName",user.FirstName??"Not Available"),
                    new Claim("LastName",user.LastName??"Not Available"),
                    new Claim("PhoneNumber",user.PhoneNumber??"Not Available"),
                    new Claim("id",user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    //new Claim(ClaimTypes.Role, roles.FirstOrDefault()),
                    new Claim("LoggedOn", DateTime.Now.ToString()),
                }),
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature),
                Issuer = _jwtsettings.Site,
                Audience = _jwtsettings.Audience,
                Expires = DateTime.Now.AddMinutes(expirationTime),               
                
            };
            tokenDescriptor.Subject.AddClaims(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            //create the token 
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddMonths(5);
            user.LastLoginDate = DateTime.Now;
            _repoWrapper.ApplicationUser.Update(user);
            await _repoWrapper.Save();

            var loggedInResponse = new LoggedInResponseDTO
            {
                Token = tokenHandler.WriteToken(token),
                Username = user.Email,
                Name = $"{user.FirstName} {user.LastName}",
                Roles = roles,
                ExpiryTime = DateTime.Now.AddMinutes(expirationTime),
                Success = true,
                RefreshToken = refreshToken
            };

            if (user.ServiceUsed != null)
            {
                var serviceList = user.ServiceUsed.Split(",").ToList();
                if (serviceList.LastOrDefault() == "")
                {
                    serviceList.RemoveAt(serviceList.Count - 1);
                }
                loggedInResponse.Services = serviceList;
            }                
            return loggedInResponse;
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {            
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, _tokenValidationParameters, out SecurityToken securityToken);
            if (!(securityToken is JwtSecurityToken jwtSecurityToken) || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");
            return principal;
        }

        public async Task<ResponseMessage> ForgotPassword(ForgotPasswordViewModel forgotPassword)
        {
            var user = await _userManager.FindByNameAsync(forgotPassword.Username);
            if (user != null)
            {
                if(!await _userManager.IsEmailConfirmedAsync(user))
                {
                    return new ResponseMessage { Message = "Kindly confirm your email address.", Status = false };
                }
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var email = user.UserName;

                var passwordResetLink = $"{Options.APIUri.HealthBancForgotPassword}?email={HttpUtility.UrlEncode(email)}&emailToken={HttpUtility.UrlEncode(token)}";

                // Email the user the verification code
                _emailSender.SendUserResetPasswordMail(forgotPassword.Username, "Reset your password", passwordResetLink);
                return new ResponseMessage { Message = "Please Check Your Mail For Further Instructions", Status = true };
            }
            return new ResponseMessage { Message = "Username Does Not Exist", Status = false };
        }

        public async Task<ResponseMessage> ResetPassword(string email, string emailToken, ResetPasswordViewModel viewModel)
        {
            var user = await _userManager.FindByEmailAsync(email);

            PasswordVerificationResult passResult = _userManager.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash, viewModel.ConfirmPassword);
            if (passResult.Equals(PasswordVerificationResult.Failed))
            {
                if (user.HashedPasswordHistory != null)
                {
                    var hashedPassword = user.HashedPasswordHistory.Split(",").ToList();
                    if (hashedPassword.LastOrDefault() == "")
                    {
                        hashedPassword.RemoveAt(hashedPassword.Count - 1);
                    }
                    foreach (var item in hashedPassword)
                    {
                        var (Verified, NeedsUpgrade) = _passwordHasher.Check(item, viewModel.ConfirmPassword);
                        if (Verified == true)
                        {
                            return new ResponseMessage { Message = "The password you entered has been used before,please try another" };
                        }
                    }
                }

                var userPassword = await _userManager.ResetPasswordAsync(user, emailToken, viewModel.Password);
                if (userPassword.Succeeded)
                {
                    var passwordHashed = _passwordHasher.Hash(viewModel.ConfirmPassword);
                    if (user.HashedPasswordHistory != null)
                    {
                        var hashedPassword = user.HashedPasswordHistory.Split(",").ToList();
                        if (hashedPassword.LastOrDefault() == "")
                        {
                            hashedPassword.RemoveAt(hashedPassword.Count - 1);
                            if (hashedPassword.Count > 3)
                            {
                                user.LockoutEnd = null;
                                await _userManager.ResetAccessFailedCountAsync(user);
                                hashedPassword.RemoveAt(0);
                                var newPaswordHash = string.Join(",", hashedPassword);
                                user.HashedPasswordHistory = $"{newPaswordHash},{passwordHashed},";
                                await _userManager.UpdateAsync(user);
                                var passwordChangehistory = new PasswordChangeHistory(user.Id, user.Email, false, true);
                                _repoWrapper.PasswordChange.Create(passwordChangehistory);
                                await _repoWrapper.Save();
                                return new ResponseMessage { Message = "Password Changed Succefully", Status = true };
                            }
                        }
                    }

                    user.LockoutEnd = null;
                    user.HashedPasswordHistory = user.HashedPasswordHistory += passwordHashed + ",";
                    await _userManager.ResetAccessFailedCountAsync(user);
                    await _userManager.UpdateAsync(user);

                    var passwordChangehistory2 = new PasswordChangeHistory(user.Id, user.Email, false, true);
                    _repoWrapper.PasswordChange.Create(passwordChangehistory2);
                    return new ResponseMessage { Message = "Password Changed Succefully", Status = true };
                }
                else if(!userPassword.Succeeded && userPassword.Errors.Any(x => x.Code == "InvalidToken"))
                {                        
                    return new ResponseMessage { Message = "Invalid Token", ResponseCode = 23 };
                }
                return new ResponseMessage { Message = userPassword.Errors.FirstOrDefault().Description, Data = userPassword.Errors};
            }
            else
            {
                return new ResponseMessage { Message = "New Password Cant Be similar with Old Password" };
            }
        }      

        public async Task<ResponseMessage> SendUserEmailVerificationAsync(ApplicationUser user)
        {
            // Get the user details
            var userIdentity = await _userManager.FindByNameAsync(user.UserName);
            var encryptedUserIdentity = userIdentity.Id.ToString();

            if (userIdentity != null)
            {
                // Generate an email verification code
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                var confirmationUrl = $"{Options.APIUri.HealthBancApiBase}v1/api/Identity/ConfirmEmail?userId={HttpUtility.UrlEncode(encryptedUserIdentity)}&emailToken={HttpUtility.UrlEncode(token)}";

                // Email the user the verification code
                _emailSender.SendUserVerificationMail(user.UserName, "Confirm your email address", confirmationUrl);
                return new ResponseMessage { Status = true };
            }
            return new ResponseMessage { Status = true, Message = "User does not exist.could not fetch user" };
        }

        private async Task ProcessInsuranceUserId(ApplicationUser user, string email)
        {
            var insuranceProfile = await _repoWrapper.InsuranceProfile.GetByEmail(email);
            if(insuranceProfile != null)
            {
                if(insuranceProfile.UserId is null)
                {
                    insuranceProfile.UserId = user.Id;
                    var newServiceString = user.ServiceUsed + ServiceNames.HealthInsured.ToString();
                    user.ServiceUsed = newServiceString;

                    _repoWrapper.ApplicationUser.Update(user);
                    _repoWrapper.InsuranceProfile.Update(insuranceProfile);
                    await _repoWrapper.Save();

                    if(insuranceProfile.ContactAddress != null)
                    {
                        var completionProfile = new InsuranceCompletionProfile(user.Id, true, true, insuranceProfile.InsuranceService);
                        _repoWrapper.InsuranceCompletionProfile.Create(completionProfile);
                    }
                }                
            }
            await Task.CompletedTask;
        }
    }
}






























//public async Task<ResponseMessage> CreateAdmin(CreateAdminRegViewModel regViewModel, string superAdminEmail, int superAdminId)
//{
//    var superAdmin = await _userManager.FindByEmailAsync(superAdminEmail);

//    var checkIfAdminExist = await _userManager.FindByNameAsync(regViewModel.Email);


//    if (checkIfAdminExist == null)
//    {
//        var user = new ApplicationUser
//        {
//            UserName = regViewModel.Email,
//            Email = regViewModel.Email,
//            FirstName = regViewModel.FirstName,
//            LastName = regViewModel.LastName,
//            PhoneNumber = regViewModel.PhoneNumber,
//            DateOfRegistration = DateTime.Now,
//            SuperAdminId = superAdminId
//        };

//        var result = await _userManager.CreateAsync(user);
//        if (result.Succeeded)
//        {
//            try
//            {
//                user.AdminId = user.Id;
//                await _userManager.UpdateAsync(user);
//                var role = await _classOrRole.GetRole(regViewModel.ClassOrRoleId);
//                await _userManager.AddToRoleAsync(user, role.Name);

//                // Generate an email verification code
//                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

//                var encryptedEmail = _encryptAndDecrypt.EncryptString(user.UserName, "hfahkbak78r32rg87griva..");
//                var encryptedToken = _encryptAndDecrypt.EncryptString(token, "hfahkbak78r32rg87griva..");

//                // TODO: Replace with APIRoutes that will contain the static routes to use

//                var confirmationUrl = $"https://pharmmall.azurewebsites.net/set-new-password/?email={HttpUtility.UrlEncode(encryptedEmail)}&emailToken={HttpUtility.UrlEncode(encryptedToken)}&destination=adminreg";
//                _emailSender.SendEmail(user.UserName, "d-6035520c662a43fba6ad2718deaedf79", confirmationUrl, superAdmin.FirstName + " " + superAdmin.LastName);


//                return new ResponseMessage
//                {
//                    Message = "Admin was invited successfully. Admin should check email for invite",
//                    Status = true
//                };
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error Occurred While Trying to Create the Admin");
//            }
//        }
//        return new ResponseMessage { Message = "Error Occurred While Trying to Create the Admin" };
//    }
//    else
//    {
//        if (checkIfAdminExist.UniqueUsername != null)
//        {
//            return new ResponseMessage { Message = "User exist with admin access, Kindly invite with another email" };
//        }
//        if (checkIfAdminExist.ServiceUsed != null)
//        {
//            if (checkIfAdminExist.ServiceUsed.Contains("Pharmmall") && checkIfAdminExist.UniqueUsername == null)
//            {
//                return new ResponseMessage { Message = "Admin exist with a company in HealthMall, Kindly invite with another email" };
//            }
//        }
//        if (checkIfAdminExist.AdminId == null && checkIfAdminExist.UniqueUsername == null)
//        {
//            checkIfAdminExist.SuperAdminId = superAdminId;
//            checkIfAdminExist.AdminId = checkIfAdminExist.Id;
//            await _userManager.UpdateAsync(checkIfAdminExist);

//            var role = await _classOrRole.GetRole(regViewModel.ClassOrRoleId);
//            await _userManager.AddToRoleAsync(checkIfAdminExist, role.Name);

//            var loginPage = $"https://pharmmall.azurewebsites.net/signin";
//            _emailSender.SendEmail(checkIfAdminExist.Email, "d-bb5d5f1175764248be51f9c1bdaf533e", loginPage, superAdmin.FirstName + " " + superAdmin.LastName);

//            return new ResponseMessage
//            {
//                Message = "Admin was invited successfully. Admin should check email for invite",
//                Status = true
//            };
//        }
//        return new ResponseMessage { Message = "Admin exist with a company in HealthMall, Kindly invite with another email" };
//    }
//}

//public async Task<ResponseMessage> AdminReg(string email, string emailToken, AdminRegViewModel regViewModel)
//{
//    var decryptedEmail = _encryptAndDecrypt.DecryptString(email, "hfahkbak78r32rg87griva..");
//    var decryptedToken = _encryptAndDecrypt.DecryptString(emailToken, "hfahkbak78r32rg87griva..");

//    var user = await _userManager.FindByEmailAsync(decryptedEmail);

//    if (user == null)
//    {
//        return new ResponseMessage { Message = "User does not exist", ResponseCode = 2 };
//    }
//    var result = await _userManager.ConfirmEmailAsync(user, decryptedToken);

//    if (result.Succeeded)
//    {
//        try
//        {
//            var token = _userManager.GeneratePasswordResetTokenAsync(user);
//            var setPassword = await _userManager.ResetPasswordAsync(user, token.Result, regViewModel.NewPassword);
//            if (setPassword.Succeeded)
//            {
//                return new ResponseMessage { Message = "Email Confirmed Successfully,Please Log In", Status = true };
//            }
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error occured While Trying to create admin");
//        }
//        return new ResponseMessage { Message = "Error occured While Trying to Create Admin " };
//    }
//    else if (!result.Succeeded && result.Errors.Any(x => x.Code == "InvalidToken"))
//    {
//        return new ResponseMessage { Message = "Invalid Token", ResponseCode = 23 };
//    }
//    return new ResponseMessage { Message = result.Errors.FirstOrDefault().Description, Data = result.Errors };
//}