using Application.Common.DTO;
using Hangfire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Interfaces
{
    public interface IEmailService
    {
        Task<bool> EmailRequest(EmailRequest emailRequest);
        [AutomaticRetry(Attempts = 0)]
        Task PaymentConfirmationEmail(string userName, string email);
        [AutomaticRetry(Attempts = 0)]
        Task PlanStepsEmail(string userName, string email, string service, string vendorName, string productName);
    }
}
