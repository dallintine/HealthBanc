using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.AdminAuth.DTO
{
    public class ADResponseRoot
    {
        public ADResponse AD_Response { get; set; }
    }

    public class Response
    {
        public string ResponseCode { get; set; }
        public string ResponseDescription { get; set; }
    }

    public class ADResponse
    {
        public string Status { get; set; }
        public Response Response { get; set; }
    }
}
