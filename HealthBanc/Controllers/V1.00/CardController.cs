using Application.Card.Commands;
using Application.Card.DTO;
using Application.Card.Queries;
using Application.Common.DTO;
using Application.Orders.DTO;
using Application.Orders.Queries;
using Application.Payment.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthBanc.Controllers.V1._00
{
    [ApiVersion("1.00")]
    public class CardController : BaseApiController
    {
        private readonly ILogger<CardController> _logger;

        public CardController(ILogger<CardController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get Cards List
        /// </summary>
        /// <param name="queryList"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        [Authorize(Roles = "User")]
        [ProducesResponseType(200, Type = typeof(BaseResponse<List<CardDTO>>))]
        public async Task<IActionResult> List([FromQuery] GetCardsQuery queryList)
        {
            return HandleResult(await Mediator.Send(queryList));
        }

        /// <summary>
        /// Update Default Card
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        [HttpPatch("[action]")]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(404, Type = typeof(BaseResponse))]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        public async Task<IActionResult> UpdateDefault(SetDefaultCardCommand command)
        {
            return HandleResult(await Mediator.Send(command));
        }

        /// <summary>
        /// Add Card
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        [HttpPatch("[action]")]
        [ProducesResponseType(400, Type = typeof(BaseResponse<CardDTO>))]
        [ProducesResponseType(404, Type = typeof(BaseResponse))]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        public async Task<IActionResult> Add(AddCardCommand command)
        {
            return HandleResult(await Mediator.Send(command));
        }

        /// <summary>
        /// Delete Card
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        [HttpDelete("[action]")]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(404, Type = typeof(BaseResponse))]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        public async Task<IActionResult> Delete(DeleteCardCommand command)
        {
            return HandleResult(await Mediator.Send(command));
        }
    }
}
