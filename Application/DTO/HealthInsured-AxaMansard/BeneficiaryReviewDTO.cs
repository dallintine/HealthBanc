using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTO.HealthInsured_AxaMansard
{
    public class BeneficiaryReviewDTO
    {
        public string TransId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string Gender { get; set; }
        public string DateOfBirth { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsRemove { get; set; }
        public Decimal Amount { get; set; }
    }
}
