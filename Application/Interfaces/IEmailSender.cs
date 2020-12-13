using Application.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IEmailSender
    {
        void SendEmail(string email,string templateId, string url, string companyName);
        void SendEmailWithObject(string email, string templateId, HelloEmail helloEmail);
        void SendInsurancePaymentReminder(string email, string templateId, string url, string userName, string premiumAmount);
    }
}
