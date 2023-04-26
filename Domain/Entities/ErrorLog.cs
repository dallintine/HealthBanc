using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class ErrorLog : BaseEntity
    {
        public string  ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string StackTrace { get; set; }
        public string Path { get; set; }
        public string Source { get; set; }

    }
}