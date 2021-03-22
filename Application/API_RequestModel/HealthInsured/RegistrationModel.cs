using System;
using System.Collections.Generic;
using System.Text;

namespace Application.API_RequestModel.HealthInsured
{
    public class RegistrationModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DOB { get; set; }
        public string Email { get; set; }
        public string PlanId { get; set; }
        public string GenderId { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
    }
}
