using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccess.DTO.AuditDTO
{
    public class AdminAuditLogDTO
    {
        public string ActionApplied { get; set; }
        public DateTime Date { get; set; }
    }
}
