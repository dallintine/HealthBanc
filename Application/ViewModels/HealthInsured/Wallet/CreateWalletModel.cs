using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Application.ViewModels.HealthInsured.Wallet
{
    public class CreateWalletModel
    {
        [Required]
        public string MobileNumber { get; set; }
        [Required]
        public string Otp { get; set; }
        [Required]
        public string Action { get; set; }
    }
}
