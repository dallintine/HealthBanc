using DataAccess.General.Implementation;
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
    public class ScheduledAxaEnrollmentRepository : BaseRepository<ScheduledAxaEnrollment>, IScheduledAxaEnrollmentRepository
    {
        public ScheduledAxaEnrollmentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<ScheduledAxaEnrollment> GetScheduledAxaEnrollmenttByJobId(string jobId)
        {
            return await _context.ScheduledAxaEnrollments.FirstOrDefaultAsync(x => x.JobId == jobId);
        }

        public async Task<ScheduledAxaEnrollment> GetScheduledAxaEnrollmenttByStatus(string status)
        {
            return await _context.ScheduledAxaEnrollments.FirstOrDefaultAsync(x => x.Status == "Processing");
        }
    }
}
