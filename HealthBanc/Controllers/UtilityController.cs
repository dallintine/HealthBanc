using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace HealthBanc.Controllers
{
    public class UtilityController : BaseApiController
    {
        private readonly IConfiguration _configuration;

        public UtilityController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("[action]")]
        public ActionResult DBExecute(string query)
        {
            string sqlConnectionString = _configuration["ConnectionStrings:DefaultConnection"];

            string script = query;

            SqlConnection connection = new SqlConnection(sqlConnectionString);

            Server server = new Server(new ServerConnection(connection));

            server.ConnectionContext.ExecuteNonQuery(script);
            return Ok();
        }
    }
}
