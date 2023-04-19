using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models
{
    public class OtpValidation : BaseEntity
    {
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string OTP { get; set; }
        public DateTimeOffset ExpiryAt { get; set; }
        public bool Status { get; set; } = true;
        public string Action { get; set; }
        public int ApplicationUserId { get; set; }
    }
}
