using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class OneTimePassword : BaseEntity
    {
        public long ApplicationUserId { get; set; }
        public string Otp { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string Action { get; set; }
        public bool Status { get; set; }
    }
}
