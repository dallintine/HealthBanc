using DataAccess.General.Implementation;
using DataAccess.HealthInsured.Interfaces;
using Domain.Models.AxaMansard_Insurance;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured.Implementation
{
    public class PaymentOnReactivationRepository : BaseRepository<PaymentOnReactivation>, IPaymentOnReactivationRepository
    {
        public PaymentOnReactivationRepository(ApplicationDbContext context) : base(context)
        {
        }
        public async Task<PaymentOnReactivation> GetScheduledPaymentByJobId(string jobId,int userId)
        {
            return await _context.PaymentOnReactivations.Include(x => x.EnrollmentOnReactivation)
                .FirstOrDefaultAsync(x =>x.JobId == jobId && x.UserId == userId);
        }
    }
}
