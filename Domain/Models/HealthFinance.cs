using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models
{
    public class HealthFinance
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string BusinessName { get; set; }
        public string BusinessType { get; set; }
        public string BusinessAddress { get; set; }
        public string Phonenumber { get; set; }
        public decimal Amount { get; set; }
        public string Comment { get; set; }
        public DateTimeOffset DateSubmitted { get; set; }
    }
}
