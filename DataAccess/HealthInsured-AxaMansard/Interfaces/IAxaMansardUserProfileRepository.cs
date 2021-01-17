using DataAccess.General.Interfaces;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured_AxaMansard.Interfaces
{
    public interface IAxaMansardUserProfileRepository : IBaseRepository<AxaMansardUserProfile>
    {
        Task<AxaMansardUserProfile> GetByUserIdAsync(int id);
        Task<AxaMansardUserProfile> GetByIdAsync(int id);
        Task<AxaMansardUserProfile> GetByEmail(string email);
    }
}
