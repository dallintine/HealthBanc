using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Controllers
{
    [Route("v1/api/[controller]")]
    [ApiController]

    public class BackendAdminController : ControllerBase
    {
        public BackendAdminController()
        {

        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetAllUsers()
        {
            return Ok();
        }
    }
}
