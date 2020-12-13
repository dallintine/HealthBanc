using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class PaymentReference
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Channel { get; set; }
        public int AxaMansardUserProfileId { get; set; }
        public int UserId { get; set; }
        public Decimal Amount { get; set; }
        public bool Active { get; set; }
        [JsonIgnore]
        public AxaMansardUserProfile AxaMansardUserProfile { get; set; }
    }
}
