using AutoMapper;
using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Request.Tokenize;
using HealthBanc.Response;
using HealthBanc.Services.Tokenization;
using HealthBanc.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HealthBanc.Controllers
{
    [Route("v1/api/[controller]")]
    [ApiController]
    public class TokenizationController : ControllerBase
    {
        private readonly TokenizationService _tokenizationService;
        private readonly IMapper _mapper;
        private readonly IAxaMansardUserProfileRepository _mansardUserProfileRepository;
        private readonly IAxaMansardCompletionRepository _completionRepository;

        public TokenizationController(TokenizationService tokenizationService,IMapper mapper, IAxaMansardUserProfileRepository mansardUserProfileRepository,
            IAxaMansardCompletionRepository completionRepository)
        {
            _tokenizationService = tokenizationService;
            _mapper = mapper;
            _mansardUserProfileRepository = mansardUserProfileRepository;
            _completionRepository = completionRepository;
        }

        /// <summary>
        /// Charge user card for tokenization process
        /// </summary>
        /// <param name="chargeCard"></param>
        /// <returns></returns>
        [ProducesResponseType(200, Type = typeof(ResponseMessage))]
        [ProducesResponseType(400, Type = typeof(ResponseMessage))]
        [HttpPost("[action]")]
        public async Task<IActionResult> ChargeCard(ChargeCardViewModel chargeCard)
        {
            if (ModelState.IsValid)
            {
                string userId = User.FindFirst(ClaimTypes.Name)?.Value;
                int Id = int.Parse(userId);

                var userAxamansardProfile = await _mansardUserProfileRepository.GetByAdminIdAsync(Id);
                if(userAxamansardProfile != null)
                {
                    var chargeCardRequest = _mapper.Map<ChargeCard>(chargeCard);
                    chargeCardRequest.email = userAxamansardProfile.Email;chargeCardRequest.amount = userAxamansardProfile.Premium.ToString();
                    chargeCardRequest.reference = "12234";
                    var cardResponse = await _tokenizationService.ChargeCard(chargeCardRequest);
                    return Ok(cardResponse);
                }
                return BadRequest(new ResponseMessage { Message = "User has not been profiled", Status = false});
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
            return BadRequest(new ResponseMessage { Data = errors, Message = "There were validation errors" });
        }

        
    }
}
