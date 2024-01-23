using Domain.Entities.Common;
using Newtonsoft.Json;

namespace Domain.Entities
{
    public class Card : BaseEntity
    {
        public string LastFourDigit { get; set; }
        public string CardType { get; set; }
        public string AuthorizationCode { get; set; }   
        public string Email { get; set; }
        public string Signature { get; set; }
        public long ApplicationUserId { get; set; }
        public string ExpMonth { get; set; }
        public string ExpYear { get; set; }
        public string Bank { get; set; }
        public string CardHolder { get; set; }
        public bool IsDefaultCard { get; set; }
    }
}
