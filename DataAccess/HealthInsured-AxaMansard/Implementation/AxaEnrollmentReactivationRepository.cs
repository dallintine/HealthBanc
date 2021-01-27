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
    public class AxaEnrollmentReactivationRepository : BaseRepository<EnrollmentOnReactivation> , IAxaEnrollmentReactivationRepository
    {
        public AxaEnrollmentReactivationRepository(ApplicationDbContext context) : base(context)
        {
        }
        public async Task<EnrollmentOnReactivation> GetScheduledAxaEnrollmenttByStatus(string status)
        {
            return await _context.EnrollmentOnReactivations.FirstOrDefaultAsync(x => x.Status == "Processing");
        }
    }
}
