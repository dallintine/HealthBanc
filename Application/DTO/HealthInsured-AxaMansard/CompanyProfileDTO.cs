using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTO.HealthInsured_AxaMansard
{
    public class CompanyProfileDTO
    {
        public string CompanyName { get; set; }
        public string CompanyEmail { get; set; }
        public string Industry { get; set; }
        public string CompanySize { get; set; }
        public string InsuranceService { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool ProfileCompleted { get; set; }
        public bool TokenizationCompleted { get; set; }
    }
}
