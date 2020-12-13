using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Application.ViewModels
{
    public class ChargeCardViewModel
    {
        [Required]
        public Card card { get; set; }
        [Required]
        public string pin { get; set; }
    }
    public class Card
    {
        [Required]
        public string cvv { get; set; }
        [Required]
        public int expiry_month { get; set; }
        [Required]
        public int expiry_year { get; set; }
        [Required]
        public string number { get; set; }
        public string type { get; set; }
    }
}
