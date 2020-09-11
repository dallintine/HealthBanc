using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HealthBanc.Domain.Models
{
    public class DebitCard
    {
        public DebitCard()
        {

        }
        public DebitCard(int userId, int axaMansardUserProfileId, int status, string lastFourDigit,string signature,string type, int tokenizationReferenceId)
        {
            UserId = userId;
            AxaMansardUserProfileId = axaMansardUserProfileId;
            Status = status;
            LastFourDigit = lastFourDigit;
            Signature = signature;
            Type = type;
            TokenizationReferenceId = tokenizationReferenceId;
        }

        public int Id { get; set; }
        public int UserId { get; set; }
        public int AxaMansardUserProfileId { get; set; }
        public int Status { get; set; }
        [JsonIgnore]
        public AxaMansardUserProfile AxaMansardUserProfile {get;set;}
        public string LastFourDigit { get; set; }
        public string Signature { get; set; }
        public string Type { get; set; }
        public int TokenizationReferenceId { get; set; }
        public TokenizationReference TokenizationReference { get; set; }
    }
}
