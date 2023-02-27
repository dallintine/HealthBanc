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
        Task CorporateInsuranceOnboarding(string email, string subject, string otp);
        Task FamilySubscription(string email, string subject, string userName);
        Task HealthInsuredCompanyDeactivation(string email, string subject, string userName, string premium);
        Task HealthInsuredFailedCompanyDebit(string email, string subject, string userName, string premium, string stopDate);
        Task HealthInsuredDeactivationNotification(string email, string subject, string userName, string premium,string info);
        Task HealthInsuredFailedDebitNotification(string email, string subject, string userName, string info, string premium);
        Task HealthInsuredSubscriptionMail(string email, string subject, string userName, string enroleeNumber, string healthCareProvider,string plan);
        Task RefreeInvitation(string email, string subject, string payee);
        Task RefreeInvitationFullDetail(string email, string subject, string payee, string userName, string enroleeNumber, string healthCareProvider, string plan);
        Task SendHealthFinanceNotification(string subject, HealthFinanceCollectionViewModel healthFinance, List<string> toEmails);
        Task SendHealthFinanceSubmissionNotification(string subject, HealthFinanceCollectionViewModel healthFinance, string toEmails);
        Task SendHealthInsuredFamilyPaymentReminder(string email, string subject, string familyHead, string familyMember);
        Task SendHealthInsuredPaymentReminder(string email, string subject, string userName,string info);
        Task SendHeliumNotification(string subject, string healthProvider, string providerType, string phonenumber, string providerEmail);
        Task SendUserResetPasswordMail(string email, string subject, string resetUrl);
        Task SendUserVerificationMail(string email, string subject, string verificationUrl);
        Task SendHealthInsuredUserVerificationMail(string email, string subject, string verificationUrl);
        Task SendHealthInsuredUserResetPasswordMail(string email, string subject, string resetUrl);
        Task CustomOneDrugStoreMail(string email, string subject, string content);
        Task CustomHealthInsuredMail(string email, string subject, string content);
        Task SendOneDrugStoreUserResetPasswordMail(string email, string subject, string resetUrl);
    }
}
