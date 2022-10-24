using Application.API_ResponseModel.Wallet;
using Application.DTO;
using Application.Interfaces;
using Application.Services;
using Application.Services.Wallet;
using Application.ViewModels.HealthInsured.Wallet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HealthBanc.Controllers.V2.Insurance
{
    [Route("v{version:apiVersion}/api/[controller]")]
    [ApiController]
    [ApiVersion("2.0")]
    public class WalletController : ControllerBase
    {
        private readonly WalletService _walletService;
        private readonly IEncryptAndDecrypt _encryptDecrypt;
        private readonly ResponseHelper _responseHelper;

        public WalletController(WalletService walletService, IEncryptAndDecrypt encryptDecrypt,ResponseHelper responseHelper)
        {
            _walletService = walletService;
            _encryptDecrypt = encryptDecrypt;
            _responseHelper = responseHelper;
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
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);

            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));

            var generateOTP = JsonConvert.DeserializeObject<GenerateWalletOTPViewModel>(decryptedString.Item2);

            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int id = int.Parse(userId);
            var response = await _walletService.GenerateOTPForExistingWallet(id, generateOTP.MobileNumber);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(response));
            if (response.Status) return Ok(data);
            return BadRequest(data);
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
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var generateOTP = JsonConvert.DeserializeObject<GenerateWalletOTPViewModel>(decryptedString.Item2);
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int id = int.Parse(userId);
            var response = await _walletService.GenerateOTPForNewWallet(id, generateOTP.MobileNumber);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(response));
            if (response.Status) return Ok(data);
            return BadRequest(data);
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
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var linkWallet = JsonConvert.DeserializeObject<LinkWalletModel>(decryptedString.Item2);
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int id = int.Parse(userId);
            var response = await _walletService.LinkWallet(id, linkWallet);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(response));
            if (response.Status) return Ok(data);
            return BadRequest(data);
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
            if (!ModelState.IsValid) return BadRequest(_responseHelper.BuildResponse(30, ModelState));
            var decryptedString = _encryptDecrypt.DecryptString(encryptedModel.Data);
            if (!decryptedString.Item1) return BadRequest(_encryptDecrypt.EncryptString(JsonConvert.SerializeObject(
                new ResponseMessage { ResponseCode = 12, Message = decryptedString.Item2 })));
            var walletModel = JsonConvert.DeserializeObject<CreateWalletModel>(decryptedString.Item2);
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int id = int.Parse(userId);
            var response = await _walletService.CreateWallet(id, walletModel);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(response));
            if (response.Status) return Ok(data);
            return BadRequest(data);
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
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(response));
            if (response.Status) return Ok(data);
            return BadRequest(data);
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
            string userId = User.FindFirst(ClaimTypes.Name)?.Value;
            int id = int.Parse(userId);
            var response = await _walletService.SwitchToWalletPayment(id);
            var data = _encryptDecrypt.EncryptString(JsonConvert.SerializeObject(response));
            if (response.Status) return Ok(data);
            return BadRequest(data);
        }
    }
}
