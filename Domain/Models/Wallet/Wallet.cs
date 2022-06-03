using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models.Wallet
{
    public class Wallet
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? InsuranceUserProfileId { get; set; }
        public int? CompanyProfileId { get; set; }
        public int? FamilyProfileId { get; set; }
        public string WalletId { get; set; }
    }
}
