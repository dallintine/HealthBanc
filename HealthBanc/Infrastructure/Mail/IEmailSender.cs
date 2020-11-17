using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Infrastructure.Mail
{
    public interface IEmailSender
    {
        void SendEmail(string email,string templateId, string url, string companyName);
        void SendEmailWithObject(string email, string templateId, EmailSender.HelloEmail helloEmail);
        void SendInsurancePaymentReminder(string email, string templateId, string url, string userName, string premiumAmount);
    }
}
