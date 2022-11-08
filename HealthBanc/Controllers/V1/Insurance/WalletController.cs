using Application.API_ResponseModel.Wallet;
using Application.DTO;
using Application.Services.Wallet;
using Application.ViewModels.HealthInsured.Wallet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using UAParser;

namespace HealthBanc.Controllers.Insurance
{
    [Route("v{version:apiVersion}/api/[controller]")]
    [ApiController]
    [ApiVersion("1.0",Deprecated = true)]
    public class WalletController : ControllerBase
    {
        private readonly WalletService _walletService;
        public StringValues agent;
        public string IpAddress;

        public WalletController(WalletService walletService, IHttpContextAccessor accessor)
        {
            _walletService = walletService;
            agent = accessor.HttpContext.Request.Headers["User-Agent"];
            IpAddress = accessor.HttpContext.Connection.RemoteIpAddress.ToString();
        }

        /// <summary>
        /// Generate OTP for existing wallet
        /// </summary>
        /// <param name="mobileNumber"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("[action]")]
        public async Task<IActionResult> GenerateOTPForExistingWallet(string mobileNumber)
        {
            var device = GetDevice();

            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int id = int.Parse(userId);
            var response = await _walletService.GenerateOTPForExistingWallet(id,mobileNumber,device,IpAddress);
            if(response.Status) return Ok(response);
            return BadRequest(response);
        }

        /// <summary>
        /// Generate OTP for a new wallet
        /// </summary>
        /// <param name="mobileNumber"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("[action]")]
        public async Task<IActionResult> GenerateOTPForNewWallet(string mobileNumber)
        {
            var device = GetDevice();

            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int id = int.Parse(userId);
            var response = await _walletService.GenerateOTPForNewWallet(id, mobileNumber, device, IpAddress);
            if (response.Status) return Ok(response);
            return BadRequest(response);
        }

        /// <summary>
        /// Validate OTP and link account to an existing wallet
        /// </summary>
        /// <param name="linkWallet"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("[action]")]
        public async Task<IActionResult> LinkWallet(LinkWalletModel linkWallet )
        {
            if (ModelState.IsValid)
            {
                var device = GetDevice();
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int id = int.Parse(userId);
                var response = await _walletService.LinkWallet(id, linkWallet, device, IpAddress);
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
        /// <param name="walletModel"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage<string>))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage<string>))]
        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("[action]")]
        public async Task<IActionResult> CreateWallet(CreateWalletModel walletModel)
        {
            if (ModelState.IsValid)
            {
                var device = GetDevice();
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
