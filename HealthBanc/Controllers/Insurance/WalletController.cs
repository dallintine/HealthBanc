using Application.DTO;
using Application.Services.Wallet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HealthBanc.Controllers.Insurance
{
    [Route("v1/api/[controller]")]
    [ApiController]
    public class WalletController : ControllerBase
    {
        private readonly WalletService _walletService;

        public WalletController(WalletService walletService)
        {
            _walletService = walletService;
        }

        /// <summary>
        /// Generate OTP for wallet
        /// </summary>
        /// <param name="mobileNumber"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("[action]")]
        public async Task<IActionResult> GenerateOTPForWallet(string mobileNumber)
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int id = int.Parse(userId);
            var response = await _walletService.GenerateOTPForWallet(id,mobileNumber);
            if(response.Status) return Ok(response);
            return BadRequest(response);
        }

        /// <summary>
        /// Validate OTP
        /// </summary>
        /// <param name="otp"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("[action]")]
        public async Task<IActionResult> ValidateOTP(string otp)
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int id = int.Parse(userId);
            var response = await _walletService.ValidateOTP(id, otp);
            if (response.Status) return Ok(response);
            return BadRequest(response);
        }

        /// <summary>
        /// Create Wallet
        /// </summary>
        /// <param name="mobileNumber"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("[action]")]
        public async Task<IActionResult> CreateWallet(string mobileNumber)
        {
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int id = int.Parse(userId);
            var response = await _walletService.CreateWallet(id, mobileNumber);
            if (response.Status) return Ok(response);
            return BadRequest(response);
        }
    }
}
