using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.CommonDTO
{
    public class EmailRequest
    {
        public EmailRequest()
        {
        }

        public EmailRequest(string email, string message, string subject, string sender)
        {
            this.Email = email;
            this.Message = message;
            this.Subject = subject;
            this.Sender = sender;
        }

        public string Email { get; set; }
        public string Message { get; set; }
        public string Subject { get; set; }
        public string Sender { get; set; }
    }
}
