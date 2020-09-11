using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.DTO.TokenizationDTO
{
    public class CardDTO
    {
        public int Id { get; set; }
        public int Status { get; set; }
        public string LastFourDigit { get; set; }
        public string Type { get; set; }
    }
}
