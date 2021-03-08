using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTO
{
    public class TransactionLogDTO
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public Decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; }
    }
}
