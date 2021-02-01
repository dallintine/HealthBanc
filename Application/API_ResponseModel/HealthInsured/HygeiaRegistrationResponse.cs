using System;
using System.Collections.Generic;
using System.Text;

namespace Application.API_ResponseModel.HealthInsured
{
    public class HygeiaRegistrationResponse
    {
        public bool Success { get; set; }
        public string MemberId { get; set; }
        public string LegacyCode { get; set; }
        public string DependantId { get; set; }
    }
}
