using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTO.HealthInsured_AxaMansard
{
    public class CompanyProfileBeneficiaryAnalyticDTO
    {
        public int ActiveBeneficiaryCount { get; set; }
        public int InactiveBeneficiaryCount { get; set; }
        public DateTime? NextPaymentDate { get; set; }
    }
}
