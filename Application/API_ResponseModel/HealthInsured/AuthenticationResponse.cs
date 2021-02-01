using System;
using System.Collections.Generic;
using System.Text;

namespace Application.API_ResponseModel.HealthInsured
{
    public class AuthenticationResponse
    {
        public bool Succeeded { get; set; }
        public string Uuid { get; set; }
        public string Auth_token { get; set; }
        public int Expires_in { get; set; }
        public object Message { get; set; }
        public string Refresh_token { get; set; }
    }
}
