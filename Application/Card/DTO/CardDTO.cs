using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Card.DTO
{
    public class CardDTO
    {
        public long Id { get; set; }
        public string LastFourDigit { get; set; }
        public string CardType { get; set; }
        public string Bank { get; set; }
        public string ExpMonth { get; set; }
        public string ExpYear { get; set; }
        public string CardHolder { get; set; }
        public bool IsDefaultCard { get; set; }
    }
}
