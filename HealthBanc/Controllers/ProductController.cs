using Application.CommonDTO;
using Application.Products;
using Microsoft.AspNetCore.Mvc;

namespace HealthBanc.Controllers
{
    public class ProductController : BaseApiController
    {
        private readonly ILogger<ProductController> _logger;

        public ProductController(ILogger<ProductController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Product List
        /// </summary>
        /// <returns></returns>
        [HttpGet("[action]")]
        [ProducesResponseType(200, Type = typeof(BaseResponse<List<ProductDTO>>))]
        public async Task<IActionResult> List()
        {
            return HandleResult(await Mediator.Send(new List.Query()));
        }
    }
}
