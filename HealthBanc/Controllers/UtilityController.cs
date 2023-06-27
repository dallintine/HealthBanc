using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Persistence.Data;

namespace HealthBanc.Controllers
{
    public class UtilityController : BaseApiController
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UtilityController(IConfiguration configuration,ApplicationDbContext context,UserManager<ApplicationUser> userManager)
        {
            _configuration = configuration;
            _context = context;
            _userManager = userManager;
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

        [HttpGet("[action]")]
        public async Task SeedData()
        {
            var email = _configuration["DefaultAdmin:Email"];
            var user = await _userManager.FindByEmailAsync($"{email}.admin");
            if (user is null)
            {
                user = new ApplicationUser()
                {
                    UserName = email,
                    Email = $"{email}.admin",
                    FirstName = _configuration["DefaultAdmin:FirstName"],
                    LastName = _configuration["DefaultAdmin:LastName"],
                    EmailConfirmed = true
                };

                var result = _userManager.CreateAsync(user).Result;

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, Roles.Admin.ToString());
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}
