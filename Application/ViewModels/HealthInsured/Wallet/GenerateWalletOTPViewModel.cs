using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Application.ViewModels.HealthInsured.Wallet
{
    public class GenerateWalletOTPViewModel
    {
        [DataType(DataType.PhoneNumber)]
        [StringLength(14, MinimumLength = 11, ErrorMessage = "Invalid phone number length")]
        [RegularExpression(@"^[+-]?([0-9]+\?[0-9]*|[0-9]+)$", ErrorMessage = "Not a valid phone number")]
        public string MobileNumber { get; set; }
    }
}
