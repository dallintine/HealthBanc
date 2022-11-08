using Application.API_ResponseModel.Wallet;
using Application.DTO;
using Application.Interfaces;
using Application.Services.Wallet;
using Application.ViewModels.HealthInsured.Wallet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using UAParser;

namespace HealthBanc.Controllers.V2.Insurance
{
    [Route("v{version:apiVersion}/api/[controller]")]
    [ApiController]
    [ApiVersion("2.0")]
    public class WalletController : ControllerBase
    {
        private readonly WalletService _walletService;
        private readonly IEncryptAndDecrypt _encryptDecrypt;
        public StringValues agent;
        public string IpAddress;
        public WalletController(WalletService walletService, IEncryptAndDecrypt encryptDecrypt, IHttpContextAccessor accessor)
        {
            _walletService = walletService;
            _encryptDecrypt = encryptDecrypt;
            agent = accessor.HttpContext.Request.Headers["User-Agent"];
            IpAddress = accessor.HttpContext.Connection.RemoteIpAddress.ToString();
        }

        /// <summary>
        /// Generate OTP for existing wallet
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("[action]")]
        [MapToApiVersion("2.0")]
        public async Task<IActionResult> GenerateOTPForExistingWallet(EncryptedModel encryptedModel)
        {
            if (ModelState.IsValid)
            {
                var device = GetDevice();

                var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
                if (!decryptedString.Item1) return BadRequest(new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 });
                var generateOTP = JsonConvert.DeserializeObject<GenerateWalletOTPViewModel>(decryptedString.Item2);

                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int id = int.Parse(userId);
                var response = await _walletService.GenerateOTPForExistingWallet(id, generateOTP.MobileNumber,device,IpAddress);
                if (response.Status) return Ok(response);
                return BadRequest(response);
            }
            //return validation errors
            var errors = new List<string>();
            var errorList = ModelState.Values.SelectMany(m => m.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            foreach (var error in errorList)
            {
                errors.Add(error);
            }
            return BadRequest(new ResponseMessage { Data = errors, Message = errors.FirstOrDefault().ToString() });
        }

        /// <summary>
        /// Generate OTP for a new wallet
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("[action]")]
        [MapToApiVersion("2.0")]
        public async Task<IActionResult> GenerateOTPForNewWallet(EncryptedModel encryptedModel)
        {
            if (ModelState.IsValid)
            {
                var device = GetDevice();

                var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
                if (!decryptedString.Item1) return BadRequest(new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 });
                var generateOTP = JsonConvert.DeserializeObject<GenerateWalletOTPViewModel>(decryptedString.Item2);
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int id = int.Parse(userId);
                var response = await _walletService.GenerateOTPForNewWallet(id, generateOTP.MobileNumber,device,IpAddress);
                if (response.Status) return Ok(response);
                return BadRequest(response);
            }
            //return validation errors
            var errors = new List<string>();
            var errorList = ModelState.Values.SelectMany(m => m.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            foreach (var error in errorList)
            {
                errors.Add(error);
            }
            return BadRequest(new ResponseMessage { Data = errors, Message = errors.FirstOrDefault().ToString() });
        }

        /// <summary>
        /// Validate OTP and link account to an existing wallet
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("[action]")]
        [MapToApiVersion("2.0")]
        public async Task<IActionResult> LinkWallet(EncryptedModel encryptedModel)
        {
            if (ModelState.IsValid)
            {
                var device = GetDevice();

                var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
                if (!decryptedString.Item1) return BadRequest(new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 });
                var linkWallet = JsonConvert.DeserializeObject<LinkWalletModel>(decryptedString.Item2);
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int id = int.Parse(userId);
                var response = await _walletService.LinkWallet(id, linkWallet,device,IpAddress);
                if (response.Status) return Ok(response);
                return BadRequest(response);
            }
            //return validation errors
            var errors = new List<string>();
            var errorList = ModelState.Values.SelectMany(m => m.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            foreach (var error in errorList)
            {
                errors.Add(error);
            }
            return BadRequest(new ResponseMessage { Data = errors, Message = errors.FirstOrDefault().ToString() });
        }

        /// <summary>
        /// Validate OTP and Create Wallet for the user
        /// </summary>
        /// <param name="encryptedModel"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<string>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage<string>))]
        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("[action]")]
        [MapToApiVersion("2.0")]
        public async Task<IActionResult> CreateWallet(EncryptedModel encryptedModel)
        {
            if (ModelState.IsValid)
            {
                var device = GetDevice();

                var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
                if (!decryptedString.Item1) return BadRequest(new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 });
                var walletModel = JsonConvert.DeserializeObject<CreateWalletModel>(decryptedString.Item2);
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int id = int.Parse(userId);
                var response = await _walletService.CreateWallet(id, walletModel,device,IpAddress);
                if (response.Status) return Ok(response);
                return BadRequest(response);
            }
            //return validation errors
            var errors = new List<string>();
            var errorList = ModelState.Values.SelectMany(m => m.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            foreach (var error in errorList)
            {
                errors.Add(error);
            }
            return BadRequest(new ResponseMessage { Data = errors, Message = errors.FirstOrDefault().ToString() });
        }

        /// <summary>
        /// Get logged User Wallet Details
        /// </summary>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<WalletValidationResponse>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage<WalletValidationResponse>))]
        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("[action]")]
        [MapToApiVersion("2.0")]
        public async Task<IActionResult> WalletDetails()
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int id = int.Parse(userId);
            var response = await _walletService.WalletDetails(id);
            if (response.Status) return Ok(response);
            return BadRequest(response);
        }

        /// <summary>
        /// Switch Payment method to wallet payment
        /// </summary>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<WalletValidationResponse>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage<WalletValidationResponse>))]
        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("[action]")]
        [MapToApiVersion("2.0")]
        public async Task<IActionResult> SwitchToWalletPayment()
        {
            var device = GetDevice();

            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int id = int.Parse(userId);
            var response = await _walletService.SwitchToWalletPayment(id,device,IpAddress);
            if (response.Status) return Ok(response);
            return BadRequest(response);
        }

        private string GetDevice()
        {
            var userAgent = agent;
            string uaString = Convert.ToString(userAgent[0]);
            var uaParser = Parser.GetDefault();
            ClientInfo c = uaParser.Parse(uaString);
            return c.UA.ToString();
        }
    }
}
