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

        Task<int> Save();
    }
}
