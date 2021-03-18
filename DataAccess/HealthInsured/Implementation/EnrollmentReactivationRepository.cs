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
    public class EnrollmentReactivationRepository : BaseRepository<EnrollmentOnReactivation> , IEnrollmentReactivationRepository
    {
        public EnrollmentReactivationRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
