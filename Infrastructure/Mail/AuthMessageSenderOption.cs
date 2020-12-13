using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Mail
{
    public class AuthMessageSenderOption
    {
        public string SendGriduser { get; set; }
        public string SendGridApiKey { get; set; }
    }
}
