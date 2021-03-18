using DataAccess.General.Implementation;
using DataAccess.HealthInsured.Interfaces;
using Domain.Models.Axa_Hygeia_Insurance;
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
    }
}
