using DataAccess.General.Interfaces;
using DataAccess.HealthInsured.Interfaces;
using DataAccess.Logs.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess
{
    public interface IRepositoryWrapper
    {
        IBeneficiaryReviewRepository BeneficiaryReview { get; }
        IInsuranceProfileRepository InsuranceProfile { get; }
        ICardRepository Card { get; }
        IInsuranceCompletionProfileRepository InsuranceCompletionProfile { get; }
        ICompanyProfileRepository CompanyProfile { get; }
        IPaymentReferenceRepository PaymentReference { get; }
        IEnrollmentOnOnboardingRepository EnrollmentOnOnboarding { get; }
        IPaymentOnReactivationRepository PaymentOnReactivation { get; }
        IEnrollmentReactivationRepository EnrollmentReactivation { get; }
        IScheduledEnrollmentRepository ScheduledEnrollment { get; }
        IScheduledPaymentRepository ScheduledPayment { get; }
        IApplicationUserRepository ApplicationUser { get; }
        IHygeiaHospitalListRepository HygeiaHospitalList { get; }
        IAxaMansardHospitalListRepository AxaMansardHospitalList { get; }
        IActivityLogRepository HealthInsuredActivityLog { get; }
        IExceptionLogRepository ExceptionLog { get; }
        IActivityLogRepository ActivityLog { get; }
        IAdminAuditLogRepository AdminAuditLog { get; }
        IEncryptedAcessTokenRepository EncryptedAcessToken { get; }
        IBackendAdminRepository BackendAdmin { get; }
        IClassOrRoleRepository ClassOrRole { get; }
        INotificationRepository Notification { get; }
        IServiceRepository Service { get; }
        IUserAuditLogRepository UserAuditLog { get; }
        IPasswordChangeRepository PasswordChange { get; }
        IHealthFinanceRepository HealthFinance { get; }
        IHMOPaymentRepository HMOPayment { get; }
        IFamilyProfileRepository FamilyProfile { get; }
        IUserSessionRepository UserSession { get; }

        Task<int> Save();
    }
}
