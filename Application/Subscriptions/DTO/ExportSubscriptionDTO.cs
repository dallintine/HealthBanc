using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Subscriptions.DTO
{
    public class ExportSubscriptionDTO
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string Amount { get; set; }
        public string CreatedAt { get; set; }
        public string Status { get; set; }
        public bool IsSuccessful { get; set; }
        public string Vendor { get; set; }
        public string Plan { get; set; }
        public string PaymentReference { get; set; }
    }
}
