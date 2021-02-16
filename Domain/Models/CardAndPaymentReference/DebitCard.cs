using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class DebitCard
    {
        public DebitCard()
        {

        }
        public DebitCard(int userId, int? insuranceUserProfileId,int? companyProfileId, int status, string lastFourDigit,string type,string cardReference,string authorization_Code)
        {
            UserId = userId;
            InsuranceUserProfileId = insuranceUserProfileId;
            CompanyProfileId = companyProfileId;
            Status = status;
            LastFourDigit = lastFourDigit;
            Type = type;
            CardReference = cardReference;
            Authorization_Code = authorization_Code;
        }

        public int Id { get; set; }
        public int UserId { get; set; }
        public int? InsuranceUserProfileId { get; set; }
        public int? CompanyProfileId { get; set; }
        public int Status { get; set; }
        public string LastFourDigit { get; set; }
        public string Type { get; set; }
        public string CardReference { get; set; }
        public string Authorization_Code { get; set; }
    }
}
