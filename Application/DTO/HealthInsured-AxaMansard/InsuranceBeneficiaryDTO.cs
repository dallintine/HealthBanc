using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTO.HealthInsured_AxaMansard
{
    public class InsuranceBeneficiaryDTO
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime? DateCreated { get; set; }
        public Decimal Amount { get; set; }
    }
}
