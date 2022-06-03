using DataAccess.General.Implementation;
using DataAccess.General.Interfaces;
using DataAccess.HealthInsured.Implementation;
using DataAccess.HealthInsured.Interfaces;
using DataAccess.Logs.Implementation;
using DataAccess.Logs.Interfaces;
using Domain.Models.Axa_Hygeia_Insurance;
using Persistence;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess
{
    public class RepositoryWrapper : IRepositoryWrapper
    {
        private ApplicationDbContext _context;
        private IBeneficiaryReviewRepository _beneficiaryReviewRepository;
        private IInsuranceProfileRepository _insuranceProfileRepository;
        private IScheduledPaymentRepository _scheduledPayment;
        private IScheduledEnrollmentRepository _scheduledEnrollment;
        private IEnrollmentReactivationRepository _enrollmentOnReactivation;
        private IPaymentOnReactivationRepository _paymentOnReactivation;
        private IEnrollmentOnOnboardingRepository _enrollmentOnOnboardingRepository;
        private IPaymentReferenceRepository _paymentReference;
        private ICompanyProfileRepository _companyProfileRepository;
        private IInsuranceCompletionProfileRepository _completionRepository;
        private ICardRepository _cardRepository;
        private IApplicationUserRepository _userRepository;
        private IAxaMansardHospitalListRepository _hospitalListRepository;
        private IHygeiaHospitalListRepository _hygeiaHospitalListRepository;
        private IActivityLogRepository _healthInsuredActivityLogRepository;
        private IExceptionLogRepository _exceptionLogRepository;
        private IActivityLogRepository _activityLogRepository;
        private IAdminAuditLogRepository _adminAuditLogRepository;
        private IEncryptedAcessTokenRepository _encryptedAcessTokenRepository;
        private IBackendAdminRepository _backendAdminRepository;
        private IClassOrRoleRepository _classOrRoleRepository;
        private INotificationRepository _notificationRepository;
        private IServiceRepository _serviceRepository;
        private IUserAuditLogRepository _userAuditLogRepository;
        private IPasswordChangeRepository _passwordChangeRepository;
        public IHealthFinanceRepository _healthFinance;
        public IHMOPaymentRepository _hmoPayment;
        public IFamilyProfileRepository _familyProfile;
        public IUserSessionRepository _userSession;
        public IOtpValidationRepository _otpValidation;
        public IWalletRepository _wallet;

        public IWalletRepository Wallet
        {
            get
            {
                if (_wallet == null)
                {
                    _wallet = new WalletRepository(_context);
                }
                return _wallet;
            }
        }

        public IOtpValidationRepository OtpValidation
        {
            get
            {
                if (_otpValidation == null)
                {
                    _otpValidation = new OtpValidationRepository(_context);
                }
                return _otpValidation;
            }
        }

        public IFamilyProfileRepository FamilyProfile
        {
            get
            {
                if (_familyProfile == null)
                {
                    _familyProfile = new FamilyProfileRepository(_context);
                }
                return _familyProfile;
            }
        }

        public IUserSessionRepository UserSession
        {
            get
            {
                if (_userSession == null)
                {
                    _userSession = new UserSessionRepository(_context);
                }
                return _userSession;
            }
        }

        public IHMOPaymentRepository HMOPayment
        {
            get
            {
                if (_hmoPayment == null)
                {
                    _hmoPayment = new HMOPaymentRepository(_context);
                }
                return _hmoPayment;
            }
        }

        public IEncryptedAcessTokenRepository EncryptedAcessToken
        {
            get
            {
                if (_encryptedAcessTokenRepository == null)
                {
                    _encryptedAcessTokenRepository = new EncryptedAcessTokenRepository(_context);
                }
                return _encryptedAcessTokenRepository;
            }
        }

        public IHealthFinanceRepository HealthFinance
        {
            get
            {
                if (_healthFinance == null)
                {
                    _healthFinance = new HealthFinanceRepository(_context);
                }
                return _healthFinance;
            }
        }

        public IPasswordChangeRepository PasswordChange
        {
            get
            {
                if (_passwordChangeRepository == null)
                {
                    _passwordChangeRepository = new PasswordChangeRepository(_context);
                }
                return _passwordChangeRepository;
            }
        }

        public IUserAuditLogRepository UserAuditLog
        {
            get
            {
                if (_userAuditLogRepository == null)
                {
                    _userAuditLogRepository = new UserAuditLogRepository(_context);
                }
                return _userAuditLogRepository;
            }
        }

        public IServiceRepository Service
        {
            get
            {
                if (_serviceRepository == null)
                {
                    _serviceRepository = new ServiceRepository(_context);
                }
                return _serviceRepository;
            }
        }

        public IClassOrRoleRepository ClassOrRole
        {
            get
            {
                if (_classOrRoleRepository == null)
                {
                    _classOrRoleRepository = new ClassOrRoleRepository(_context);
                }
                return _classOrRoleRepository;
            }
        }

        public INotificationRepository Notification
        {
            get
            {
                if (_notificationRepository == null)
                {
                    _notificationRepository = new NotificationRepository(_context);
                }
                return _notificationRepository;
            }
        }

        public IBackendAdminRepository BackendAdmin
        {
            get
            {
                if (_backendAdminRepository == null)
                {
                    _backendAdminRepository = new BackendAdminRepository(_context);
                }
                return _backendAdminRepository;
            }
        }

        public IAdminAuditLogRepository AdminAuditLog
        {
            get
            {
                if (_adminAuditLogRepository == null)
                {
                    _adminAuditLogRepository = new AdminAuditLogRepository(_context);
                }
                return _adminAuditLogRepository;
            }
        }

        public IApplicationUserRepository ApplicationUser
        {
            get
            {
                if (_userRepository == null)
                {
                    _userRepository = new ApplicationUserRepository(_context);
                }
                return _userRepository;
            }
        }

        public IActivityLogRepository ActivityLog
        {
            get
            {
                if (_activityLogRepository == null)
                {
                    _activityLogRepository = new ActivityLogRepository(_context);
                }
                return _activityLogRepository;
            }
        }

        public IExceptionLogRepository ExceptionLog
        {
            get
            {
                if (_exceptionLogRepository == null)
                {
                    _exceptionLogRepository = new ExceptionLogRepository(_context);
                }
                return _exceptionLogRepository;
            }
        }

        public IActivityLogRepository HealthInsuredActivityLog
        {
            get
            {
                if (_healthInsuredActivityLogRepository == null)
                {
                    _healthInsuredActivityLogRepository = new ActivityLogRepository(_context);
                }
                return _healthInsuredActivityLogRepository;
            }
        }

        public IHygeiaHospitalListRepository HygeiaHospitalList
        {
            get
            {
                if (_hygeiaHospitalListRepository == null)
                {
                    _hygeiaHospitalListRepository = new HygeiaHospitalListRepository(_context);
                }
                return _hygeiaHospitalListRepository;
            }
        }

        public IAxaMansardHospitalListRepository AxaMansardHospitalList
        {
            get
            {
                if (_hospitalListRepository == null)
                {
                    _hospitalListRepository = new AxaMansardHospitalListRepository(_context);
                }
                return _hospitalListRepository;
            }
        }
        public ICardRepository Card
        {
            get
            {
                if (_cardRepository == null)
                {
                    _cardRepository = new CardRepository(_context);
                }
                return _cardRepository;
            }
        }
        public IInsuranceCompletionProfileRepository InsuranceCompletionProfile
        {
            get
            {
                if (_completionRepository == null)
                {
                    _completionRepository = new InsuranceCompletionProfileRepository(_context);
                }
                return _completionRepository;
            }
        }
        public ICompanyProfileRepository CompanyProfile
        {
            get
            {
                if (_companyProfileRepository == null)
                {
                    _companyProfileRepository = new CompanyProfileRepository(_context);
                }
                return _companyProfileRepository;
            }
        }

        public IPaymentReferenceRepository PaymentReference
        {
            get
            {
                if (_paymentReference == null)
                {
                    _paymentReference = new PaymentReferenceRepository(_context);
                }
                return _paymentReference;
            }
        }

        public IEnrollmentOnOnboardingRepository EnrollmentOnOnboarding
        {
            get
            {
                if (_enrollmentOnOnboardingRepository == null)
                {
                    _enrollmentOnOnboardingRepository = new EnrollmentOnOnboardingRepository(_context);
                }
                return _enrollmentOnOnboardingRepository;
            }
        }

        public IPaymentOnReactivationRepository PaymentOnReactivation
        {
            get
            {
                if (_paymentOnReactivation == null)
                {
                    _paymentOnReactivation = new PaymentOnReactivationRepository(_context);
                }
                return _paymentOnReactivation;
            }
        }

        public IEnrollmentReactivationRepository EnrollmentReactivation
        {
            get
            {
                if (_enrollmentOnReactivation == null)
                {
                    _enrollmentOnReactivation = new EnrollmentReactivationRepository(_context);
                }
                return _enrollmentOnReactivation;
            }
        }

        public IScheduledEnrollmentRepository ScheduledEnrollment
        {
            get
            {
                if (_scheduledEnrollment == null)
                {
                    _scheduledEnrollment = new ScheduledEnrollmentRepository(_context);
                }
                return _scheduledEnrollment;
            }
        }

        public IScheduledPaymentRepository ScheduledPayment
        {
            get
            {
                if (_scheduledPayment == null)
                {
                    _scheduledPayment = new ScheduledPaymentRepository(_context);
                }
                return _scheduledPayment;
            }
        }

        public IBeneficiaryReviewRepository BeneficiaryReview
        {
            get
            {
                if (_beneficiaryReviewRepository == null)
                {
                    _beneficiaryReviewRepository = new BeneficiaryReviewRepository(_context);
                }
                return _beneficiaryReviewRepository;
            }
        }
        public IInsuranceProfileRepository InsuranceProfile
        {
            get
            {
                if (_insuranceProfileRepository == null)
                {
                    _insuranceProfileRepository = new InsuranceProfileRepository(_context);
                }
                return _insuranceProfileRepository;
            }
        }
        public RepositoryWrapper(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<int> Save()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
