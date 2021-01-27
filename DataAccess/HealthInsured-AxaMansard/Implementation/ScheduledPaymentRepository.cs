using DataAccess.General.Implementation;
using DataAccess.General.Interfaces;
using DataAccess.HealthInsured_AxaMansard.Interfaces;
using Domain.Models.AxaMansard_Insurance;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured_AxaMansard.Implementation
{
    public class ScheduledPaymentRepository : BaseRepository<ScheduledPayment>, IScheduledPaymentRepository
    {
        public ScheduledPaymentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<ScheduledPayment> GetScheduledPaymentByJobId(string jobId)
        {
            return await _context.ScheduledPayments.Include(x => x.ScheduledEnrollment).FirstOrDefaultAsync(x => x.JobId == jobId && x.Status == "Processing");
        }
    }
}
