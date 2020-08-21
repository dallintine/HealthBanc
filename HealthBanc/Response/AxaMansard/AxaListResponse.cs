using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Response.AxaMansard
{
    public class AxaListResponse
    {
        public string Code { get; set; }

        public string Class { get; set; }
    }

    public class AxaListResponseRoot
    {
        public List<AxaListResponse> AxaListResponse { get; set; }
    }
}
