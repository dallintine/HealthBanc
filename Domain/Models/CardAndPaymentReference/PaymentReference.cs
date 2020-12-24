using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class PaymentReference
    {
        public PaymentReference()
        {

        }
        public PaymentReference(string refernce, int axaMansardUserProfileId, int userId, decimal amount)
        {
            Date = DateTime.Now;
            Channel = "HealthBanc-Axamansard";
            Refernce = refernce;
            AxaMansardUserProfileId = axaMansardUserProfileId;
            UserId = userId;
            Amount = amount;
            Active = true;
        }

        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Channel { get; set; }
        public string Refernce { get; set; }
        public int AxaMansardUserProfileId { get; set; }
        public int UserId { get; set; }
        public Decimal Amount { get; set; }
        public bool Active { get; set; }
    }
}
