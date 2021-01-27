using DataAccess.General.Interfaces;
using Domain.Models.AxaMansard_Insurance;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured_AxaMansard.Interfaces
{
    public interface IScheduledAxaEnrollmentRepository : IBaseRepository<ScheduledEnrollment>
    {
        Task<ScheduledEnrollment> GetScheduledAxaEnrollmenttByJobId(string jobId);
    }
}
