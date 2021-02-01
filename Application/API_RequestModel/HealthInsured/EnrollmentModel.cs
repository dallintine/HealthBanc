using System;
using System.Collections.Generic;
using System.Text;

namespace Application.API_RequestModel.HealthInsured
{
    public class EnrollmentModel
    {
        public string EntityCode { get; set; }
        public string EnrollmentNo { get; set; }
        public string Surname { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string PlanId { get; set; }
        public string MaritalStatus { get; set; }
        public string Gender { get; set; }
        public string Dob { get; set; }
        public string MobileNo { get; set; }
        public string State { get; set; }
        public string Lga { get; set; }
        public string Hospital { get; set; }
        public string Email { get; set; }
    }
}
