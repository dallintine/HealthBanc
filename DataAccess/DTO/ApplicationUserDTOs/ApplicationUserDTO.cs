using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.DTO.ApplicationUserDTOs
{
    public class ApplicationUserDTO
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfRegistration { get; set; }
        public string ServiceUsed { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
    }
}