using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using UAParser;

namespace Infrastructure.Services
{
    public class BaseService
    {
        public string IpAddress;
        public string Device;
        public List<Claim> Claims;
        public BaseService(IHttpContextAccessor accessor)
        {
            IpAddress = accessor?.HttpContext != null ? accessor?.HttpContext?.Connection.RemoteIpAddress.ToString() : null;
            Device = accessor?.HttpContext != null ? GetDevice(accessor.HttpContext.Request.Headers["User-Agent"]) : null;
            Claims = accessor?.HttpContext != null ? accessor.HttpContext.User?.Claims?.ToList() : null;
        }

        private static string GetDevice(StringValues userAgent)
        {
            string uaString = Convert.ToString(userAgent[0]);
            var uaParser = Parser.GetDefault();
            ClientInfo c = uaParser.Parse(uaString);
            return c.UA.ToString();
        }
    }
}
