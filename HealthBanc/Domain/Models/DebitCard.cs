using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Domain.Models
{
    public class DebitCard
    {
        public DebitCard()
        {

        }
        public DebitCard(int userId, int axaMansardUserProfileId, int status, string lastFourDigit, int tokenizationReferenceId)
        {
            UserId = userId;
            AxaMansardUserProfileId = axaMansardUserProfileId;
            Status = status;
            LastFourDigit = lastFourDigit;
            TokenizationReferenceId = tokenizationReferenceId;
        }

        public int Id { get; set; }
        public int UserId { get; set; }
        public int AxaMansardUserProfileId { get; set; }
        public int Status { get; set; }
        public AxaMansardUserProfile AxaMansardUserProfile {get;set;}
        public string LastFourDigit { get; set; }
        public int TokenizationReferenceId { get; set; }
        public TokenizationReference TokenizationReference { get; set; }
    }
}
