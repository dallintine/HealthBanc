using DataAccess.General.Interfaces;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured_AxaMansard.Interfaces
{
    public interface IAxaMansardCompletionRepository : IBaseRepository<AxaMansardCompletionProfile>
    {
        Task<AxaMansardCompletionProfile> GetCompletionStateByUserId(int id);
    }
}
