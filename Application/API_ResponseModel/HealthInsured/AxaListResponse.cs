using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.API_ResponseModel.HealthInsured
{ 
    public class AxaListResponse
    {
        public string Code { get; set; }

        public string Text { get; set; }
    }

    public class AxaListResponseRoot
    {
        public List<AxaListResponse> AxaListResponse { get; set; }
    }
}
