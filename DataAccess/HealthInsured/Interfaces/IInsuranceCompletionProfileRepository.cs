using DataAccess.General.Interfaces;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured.Interfaces
{
    public interface IInsuranceCompletionProfileRepository : IBaseRepository<InsuranceCompletionProfile>
    {
        Task<InsuranceCompletionProfile> GetCompletionStateByUserId(int id);
    }
}
