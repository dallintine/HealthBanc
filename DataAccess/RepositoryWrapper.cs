using DataAccess.General.Implementation;
using DataAccess.General.Interfaces;
using DataAccess.HealthInsured.Implementation;
using DataAccess.HealthInsured.Interfaces;
using Domain.Models.AxaMansard_Insurance;
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
        private IHealthInsuredActivityLogRepository _healthInsuredActivityLogRepository;

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

        public IHealthInsuredActivityLogRepository HealthInsuredActivityLog
        {
            get
            {
                if (_healthInsuredActivityLogRepository == null)
                {
                    _healthInsuredActivityLogRepository = new HealthInsuredActivityLogRepository(_context);
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
