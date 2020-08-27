using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Domain.Models
{
    public class TestUserProfile
    {
        public int Id { get; set; }
        public string TransId { get; set; }
        public string FirstName { get; set; }
        public string SurnName { get; set; }
        public string OtherName { get; set; }
        public string MaritalStatus { get; set; }
        public string Address { get; set; }
        public string DateOfBirth { get; set; }
        public string NextofKin { get; set; }
        public string PhoneNumber { get; set; }
        public string Plan { get; set; }
        public string Sex { get; set; }
    }
}
