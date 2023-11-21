using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Persistence.Data;

namespace HealthBanc.Controllers.V1._00
{

    [ApiVersion("1.00")]
    public class UtilityController : BaseApiController
    {
        private readonly IConfiguration _configuration;

        public UtilityController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("[action]")]
        [Authorize(Roles = "Admin")]
        public ActionResult DBExecute(string query)
        {
            string sqlConnectionString = _configuration["ConnectionStrings:DefaultConnection"];

            string script = query;

            SqlConnection connection = new SqlConnection(sqlConnectionString);

            Server server = new Server(new ServerConnection(connection));

            server.ConnectionContext.ExecuteNonQuery(script);
            return Ok();
        }

        [HttpGet("[action]")]
        [Authorize(Roles = "Admin")]
        public ActionResult HangExecute(string query)
        {
            string sqlConnectionString = _configuration["ConnectionStrings:BackgroundConnection"];

            string script = query;

            SqlConnection connection = new SqlConnection(sqlConnectionString);

            Server server = new Server(new ServerConnection(connection));

            server.ConnectionContext.ExecuteNonQuery(script);
            return Ok();
        }
    }
}
