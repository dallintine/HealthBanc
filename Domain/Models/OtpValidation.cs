using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models
{
    public class OtpValidation
    {
        public long Id { get; set; }
        public string PhoneNumber { get; set; }
        public string OTP { get; set; }
        public DateTimeOffset GeneratedDate { get; set; }
        public DateTimeOffset ExpiredDate { get; set; }
        public bool Status { get; set; } = true;
        public int ApplicationUserId { get; set; }
    }
}
