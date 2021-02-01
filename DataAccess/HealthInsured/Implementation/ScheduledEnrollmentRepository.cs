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
    public class ScheduledEnrollmentRepository : BaseRepository<ScheduledEnrollment>, IScheduledEnrollmentRepository
    {
        public ScheduledEnrollmentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<ScheduledEnrollment> GetScheduledAxaEnrollmenttByJobId(string jobId)
        {
            return await _context.ScheduledEnrollments.FirstOrDefaultAsync(x => x.JobId == jobId);
        }
    }
}
