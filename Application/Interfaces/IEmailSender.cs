using Application.DTO;
using Application.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IEmailSender
    {
        void CorporateInsuranceOnboarding(string email, string subject, string otp);
        void CustomMail(string email, string subject, string content);
        void FamilySubscription(string email, string subject, string userName);
        void HealthInsuredCompanyDeactivation(string email, string subject, string userName, string premium);
        void HealthInsuredFailedCompanyDebit(string email, string subject, string userName, string premium, string stopDate);
        void HealthInsuredDeactivationNotification(string email, string subject, string userName, string premium,string info);
        void HealthInsuredFailedDebitNotification(string email, string subject, string userName, string info, string premium);
        void HealthInsuredSubscriptionMail(string email, string subject, string userName, string enroleeNumber, string healthCareProvider,string plan);
        void RefreeInvitation(string email, string subject, string payee);
        void RefreeInvitationFullDetail(string email, string subject, string payee, string userName, string enroleeNumber, string healthCareProvider, string plan);
        void SendHealthFinanceNotification(string subject, HealthFinanceCollectionViewModel healthFinance, List<string> toEmails);
        void SendHealthFinanceSubmissionNotification(string subject, HealthFinanceCollectionViewModel healthFinance, string toEmails);
        void SendHealthInsuredFamilyPaymentReminder(string email, string subject, string familyHead, string familyMember);
        void SendHealthInsuredPaymentReminder(string email, string subject, string userName);
        void SendHeliumNotification(string subject, string healthProvider, string providerType, string phonenumber, string providerEmail);
        void SendUserResetPasswordMail(string email, string subject, string resetUrl);
        void SendUserVerificationMail(string email, string subject, string verificationUrl);
    }
}
