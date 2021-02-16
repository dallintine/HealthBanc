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
        public PaymentReference(string refernce, int? insuranceUserProfileId,int? companyProfileId, int userId, decimal amount,string status)
        {
            Date = DateTime.Now;
            Channel = "HealthBanc-Axamansard";
            Refernce = refernce;
            InsuranceUserProfileId = insuranceUserProfileId;
            CompanyProfileId = companyProfileId;
            UserId = userId;
            Amount = amount;
            Status = status;
        }

        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Channel { get; set; }
        public string Refernce { get; set; }
        public int? InsuranceUserProfileId { get; set; }
        public int? CompanyProfileId { get; set; }
        public int UserId { get; set; }
        public Decimal Amount { get; set; }
        public string Status { get; set; }
    }
}
