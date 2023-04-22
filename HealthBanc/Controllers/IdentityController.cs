using Application.CommonDTO;
using Application.Identity;
using Infrastructure.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace HealthBanc.Controllers
{
    public class IdentityController : BaseApiController
    {
        public IdentityController()
        {

        }

        /// <summary>
        /// Register User
        /// </summary>
        /// <param name="registerUser"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        public async Task<IActionResult> RegisterUser(Register.Command command)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(command));
        }
    }
}
