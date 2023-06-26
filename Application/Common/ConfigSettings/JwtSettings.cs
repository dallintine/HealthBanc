using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.ConfigSettings
{
    public class JwtSettings
    {
        public string Site { get; set; }
        public string Audience { get; set; }
        public double ExpirationTime { get; set; }
        public string Secret { get; set; }
    }
}
