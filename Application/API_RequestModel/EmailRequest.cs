using System;
using System.Collections.Generic;
using System.Text;

namespace Application.API_RequestModel
{
    public class EmailRequest
    {
        public EmailRequest()
        {
        }

        public EmailRequest(string email, string message, string subject, string sender)
        {
            this.email = email;
            this.message = message;
            this.subject = subject;
            this.sender = sender;
        }

        public string email { get; set; }
        public string message { get; set; }
        public string subject { get; set; }
        public string sender { get; set; }
    }
}
