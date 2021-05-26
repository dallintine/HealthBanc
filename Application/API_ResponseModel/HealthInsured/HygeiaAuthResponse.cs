using System;
using System.Collections.Generic;
using System.Text;

namespace Application.API_ResponseModel.HealthInsured
{
    public class HygeiaAuthResponse
    {
        public string Access_Token { get; set; }
        public string Token_Type { get; set; }
        public string Expires_In { get; set; }
    }
}
