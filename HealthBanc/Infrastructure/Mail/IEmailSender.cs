using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Infrastructure.Mail
{
    public interface IEmailSender
    {
        void SendEmail(string email,string templateId, string url);
    }
}
