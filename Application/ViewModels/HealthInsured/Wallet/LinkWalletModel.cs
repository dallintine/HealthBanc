using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Application.ViewModels.HealthInsured.Wallet
{
    public class LinkWalletModel
    {
        [Required]
        public string OTP { get; set; }
        [Required]
        public string Action { get; set; }  
    }
}
