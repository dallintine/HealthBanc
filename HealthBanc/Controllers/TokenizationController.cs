using AutoMapper;
using HealthBanc.Request.Tokenize;
using HealthBanc.Response;
using HealthBanc.Services.Tokenization;
using HealthBanc.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Controllers
{
    [Route("v1/api/[controller]")]
    [ApiController]
    public class TokenizationController : ControllerBase
    {
        private readonly TokenizationService _tokenizationService;
        private readonly IMapper _mapper;

        public TokenizationController(TokenizationService tokenizationService,IMapper mapper)
        {
            _tokenizationService = tokenizationService;
            _mapper = mapper;
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> ChargeCard(ChargeCardViewModel chargeCard)
        {
            if (ModelState.IsValid)
            {
                var chargeCardRequest = _mapper.Map<ChargeCard>(chargeCard);
                chargeCardRequest.reference = "12234";
                var cardResponse = await _tokenizationService.ChargeCard(chargeCardRequest);
                return Ok(cardResponse);
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
