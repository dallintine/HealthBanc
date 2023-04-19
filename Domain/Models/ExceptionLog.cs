using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class ExceptionLog
    {
        public ExceptionLog()
        {

        }
        public ExceptionLog(string errorCode, string errorMessage, string source, string link, DateTime errorDate, string stackTrace, string path)
        {
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
            Source = source;
            Link = link;
            ErrorDate = errorDate;
            StackTrace = stackTrace;
            Path = path;
        }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string Source { get; set; }
        public string Link { get; set; }
        public DateTime ErrorDate { get; set; }
        public string StackTrace { get; set; }
        public string Path { get; set; }
    }
}
