using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Customer.DTO
{
    public class CustomerDTO
    {
        public string CustomerID { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}