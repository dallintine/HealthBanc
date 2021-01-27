using DataAccess.General.Interfaces;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured_AxaMansard.Interfaces
{
    public interface IAxaMansardUserProfileRepository : IBaseRepository<InsuranceUserProfile>
    {
        Task<InsuranceUserProfile> GetByUserIdAsync(int id);
        Task<InsuranceUserProfile> GetByIdAsync(int id);
        Task<InsuranceUserProfile> GetByEmail(string email);
    }
}
