using Application.CommonDTO;
using Application.Image;
using Infrastructure.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace HealthBanc.Controllers
{
    public class ImageController : BaseApiController
    {
        public ImageController()
        {

        }

        /// <summary>
        /// Upload Image
        /// </summary>
        /// <param name="createCommand"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [ProducesResponseType(200, Type = typeof(BaseResponse))]
        [ProducesResponseType(400, Type = typeof(BaseResponse))]
        public async Task<IActionResult> Create(Create.Command createCommand)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseHelper.BuildResponse("30", ModelState));
            return HandleResult(await Mediator.Send(createCommand));
        }
    }
}
