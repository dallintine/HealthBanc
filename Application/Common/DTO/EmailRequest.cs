using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.DTO
{
    public class EmailRequest
    {
        public EmailRequest()
        {
        }

        public EmailRequest(string email, string message, string subject, string sender)
        {
            Email = email;
            Message = message;
            Subject = subject;
            Sender = sender;
        }

        public string Email { get; set; }
        public string Message { get; set; }
        public string Subject { get; set; }
        public string Sender { get; set; }
    }
}
