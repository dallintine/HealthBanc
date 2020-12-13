using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTO
{
    public class CardDTO
    {
        public int Id { get; set; }
        public int Status { get; set; }
        public string LastFourDigit { get; set; }
        public string Type { get; set; }
    }
}
