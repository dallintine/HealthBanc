using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using UAParser;

namespace Application.Services
{
    public class BaseService
    {
        public string IpAddress;
        public string Device;
        public BaseService(IHttpContextAccessor accessor)
        {
            IpAddress = accessor.HttpContext.Connection.RemoteIpAddress.ToString();
            Device = GetDevice(accessor.HttpContext.Request.Headers["User-Agent"]);
        }

        private static string GetDevice(StringValues userAgent)
        {
            string uaString = Convert.ToString(userAgent[0]);
            var uaParser = Parser.GetDefault();
            ClientInfo c = uaParser.Parse(uaString);
            return $"{c.OS}-{c.UA}";
        }
    }
}
