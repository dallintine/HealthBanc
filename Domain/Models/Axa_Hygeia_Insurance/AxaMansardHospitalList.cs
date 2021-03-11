using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models.Axa_Hygeia_Insurance
{
    public class AxaMansardHospitalList
    {
        public int Id { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public string HospitalName { get; set; }
        public string Address { get; set; }
        public string Specialisation { get; set; }
    }
}
