using Application.CommonDTO;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HealthBanc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BaseApiController : ControllerBase
    {
        private IMediator _mediator;
        protected IMediator Mediator => _mediator ??= HttpContext.RequestServices.GetService<IMediator>();

        //protected ActionResult HandleResult<T>(BaseResponse<T> result)
        //{
        //    if (result.Code == "00") return Ok(result);
        //    if (result.Code == "25")  return NotFound(result);
        //    return BadRequest(result);
        //}

        protected ActionResult HandleResult(BaseResponse result)
        {
            if (result.Code == "00") return Ok(result);
            if (result.Code == "25") return NotFound(result);
            return BadRequest(result);
        }
    }
}
