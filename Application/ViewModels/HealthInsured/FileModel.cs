using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models
{
    public class FileModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string Gender { get; set; }
        public string DateOfBirth { get; set; }
        public string PhoneNumber { get; set; }
        public int CompanyProfileId { get; set; }
    }
}
