using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.API_ResponseModel.HealthInsured_AxaMansard
{
    public class AxaResponse
    {
		public string IsSuccessful { get; set; }
		public string Message { get; set; }
		public string ReturnCode { get; set; }
	}
}
