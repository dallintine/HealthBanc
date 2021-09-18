using System;
using System.Collections.Generic;
using System.Text;

namespace Application.API_RequestModel.HealthInsured
{
    public class AxaDeactivation
    {
        public AxaDeactivation()
        {
        }

        public AxaDeactivation(string enrolleeCode, string entityCode)
        {
            EnrolleeCode = enrolleeCode;
            EntityCode = entityCode;
        }

        public string EnrolleeCode { get; set; }
        public string EntityCode { get; set; }
    }
}
